// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Cassandra.Migrations
{
    public class CassandraHistoryRepository : ICassandraHistoryRepository
    {
        public const string DefaultTableName = "__EFMigrationsHistory";
        private readonly LazyRef<IModel> _model;
        private readonly LazyRef<string> _migrationIdColumnName;
        private readonly LazyRef<string> _productVersionColumnName;

        public CassandraHistoryRepository(HistoryRepositoryDependencies dependencies)
        {
            Dependencies = dependencies;
            var relationalOptions = RelationalOptionsExtension.Extract(dependencies.Options);
            TableName = relationalOptions?.MigrationsHistoryTableName ?? DefaultTableName;
            TableSchema = relationalOptions?.MigrationsHistoryTableSchema;
            _model = new LazyRef<IModel>(
                () =>
                {
                    var conventionSet = Dependencies.CoreConventionSetBuilder.CreateConventionSet();
                    var modelBuilder = new ModelBuilder(Dependencies.ConventionSetBuilder.AddConventions(conventionSet));
                    modelBuilder.Entity<CassandraHistoryRow>(
                        x =>
                        {
                            ConfigureTable(x);
                            x.ToTable(TableName, TableSchema);
                        });

                    return modelBuilder.Model;
                });
            var entityType = new LazyRef<IEntityType>(() => _model.Value.FindEntityType(typeof(CassandraHistoryRow)));
            _migrationIdColumnName = new LazyRef<string>(
                () => entityType.Value.FindProperty(nameof(CassandraHistoryRow.MigrationId)).Relational().ColumnName);
            _productVersionColumnName = new LazyRef<string>(
                () => entityType.Value.FindProperty(nameof(CassandraHistoryRow.ProductVersion)).Relational().ColumnName);
        }

        protected virtual HistoryRepositoryDependencies Dependencies { get; }
        protected virtual ISqlGenerationHelper SqlGenerationHelper => Dependencies.SqlGenerationHelper;
        protected virtual string MigrationIdColumnName => _migrationIdColumnName.Value;
        protected virtual string ProductVersionColumnName => _productVersionColumnName.Value;
        protected virtual string TableName { get; }
        protected virtual string TableSchema { get; }
        protected virtual string ExistsScript => $"SELECT count(*) FROM system_schema.tables WHERE keyspace_name='{TableSchema}' and table_name='{TableName}'";

        public bool Exists() => Dependencies.DatabaseCreator.Exists()
               && InterpretExistsResult(
                   Dependencies.RawSqlCommandBuilder.Build(ExistsScript).ExecuteScalar(Dependencies.Connection));

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default) =>
            await Dependencies.DatabaseCreator.ExistsAsync(cancellationToken)
               && InterpretExistsResult(
                   await Dependencies.RawSqlCommandBuilder.Build(ExistsScript).ExecuteScalarAsync(
                       Dependencies.Connection, cancellationToken: cancellationToken));

        public IReadOnlyList<HistoryRow> GetAppliedMigrations()
        {
            var rows = new List<HistoryRow>();
            if (Exists())
            {
                var command = Dependencies.RawSqlCommandBuilder.Build(GetAppliedMigrationsSql);
                using (var reader = command.ExecuteReader(Dependencies.Connection))
                {
                    while (reader.Read())
                    {
                        rows.Add(new HistoryRow(reader.DbDataReader.GetString(0), reader.DbDataReader.GetString(1)));
                    }
                }
            }

            return rows;
        }

        public async Task<IReadOnlyList<HistoryRow>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
        {
            var rows = new List<HistoryRow>();
            if (await ExistsAsync(cancellationToken))
            {
                var command = Dependencies.RawSqlCommandBuilder.Build(GetAppliedMigrationsSql);
                using (var reader = await command.ExecuteReaderAsync(Dependencies.Connection, cancellationToken: cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        rows.Add(new HistoryRow(reader.DbDataReader.GetString(0), reader.DbDataReader.GetString(1)));
                    }
                }
            }

            return rows;
        }

        public string GetBeginIfExistsScript(string migrationId)
        {
            return string.Empty;
        }

        public string GetBeginIfNotExistsScript(string migrationId)
        {
            return string.Empty;
        }

        public string GetCreateIfNotExistsScript()
        {
            return string.Empty;
        }

        public IEnumerable<string> GetCreateScripts()
        {
            var operations = Dependencies.ModelDiffer.GetDifferences(null, _model.Value);
            var commandList = Dependencies.MigrationsSqlGenerator.Generate(operations, _model.Value);
            return commandList.Select(c => c.CommandText);
        }

        public string GetCreateScript()
        {
            throw new NotSupportedException();
        }

        public string GetDeleteScript(string migrationId)
        {
            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
            return new StringBuilder().Append("DELETE FROM ")
                .AppendLine(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
                .Append("WHERE ")
                .Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
                .Append(" = ")
                .Append(stringTypeMapping.GenerateSqlLiteral(migrationId))
                .AppendLine(SqlGenerationHelper.StatementTerminator)
                .ToString();
        }

        public string GetEndIfScript()
        {
            return string.Empty;
        }

        public string GetInsertScript(HistoryRow row)
        {
            var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
            return new StringBuilder().Append("INSERT INTO ")
                .Append(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
                .Append(" (")
                .Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
                .Append(", ")
                .Append(SqlGenerationHelper.DelimitIdentifier(ProductVersionColumnName))
                .AppendLine(")")
                .Append("VALUES (")
                .Append(stringTypeMapping.GenerateSqlLiteral(row.MigrationId))
                .Append(", ")
                .Append(stringTypeMapping.GenerateSqlLiteral(row.ProductVersion))
                .Append(")")
                .AppendLine(SqlGenerationHelper.StatementTerminator)
                .ToString();
        }

        protected virtual void ConfigureTable(EntityTypeBuilder<CassandraHistoryRow> history)
        {
            history.ToTable(DefaultTableName);
            history.HasKey(h => h.MigrationId);
            history.Property(h => h.MigrationId);
            history.Property(h => h.ProductVersion);
        }

        protected virtual string GetAppliedMigrationsSql
            => new StringBuilder()
                .Append("SELECT ")
                .Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
                .Append(", ")
                .AppendLine(SqlGenerationHelper.DelimitIdentifier(ProductVersionColumnName))
                .Append("FROM ")
                .AppendLine(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
                .Append(SqlGenerationHelper.StatementTerminator)
                .ToString();

        protected bool InterpretExistsResult(object value)
        {
            return (Int64)value > 0;
        }
    }
}

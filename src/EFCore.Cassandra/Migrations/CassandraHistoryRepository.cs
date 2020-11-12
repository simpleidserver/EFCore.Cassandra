// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
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
        private IModel _model;
        private string _migrationIdColumnName;
        private string _productVersionColumnName;

        public CassandraHistoryRepository(RelationalConnectionDependencies relationalConnectionDependencies, HistoryRepositoryDependencies dependencies)
        {
            var cassandraOptionsExtension = CassandraOptionsExtension.Extract(relationalConnectionDependencies.ContextOptions);
            Dependencies = dependencies;
            var relationalOptions = RelationalOptionsExtension.Extract(dependencies.Options);
            TableName = relationalOptions?.MigrationsHistoryTableName ?? DefaultTableName;
            TableSchema = cassandraOptionsExtension.DefaultKeyspace;
            EnsureModel();
        }

        protected virtual HistoryRepositoryDependencies Dependencies { get; }
        protected virtual ISqlGenerationHelper SqlGenerationHelper => Dependencies.SqlGenerationHelper;
        protected virtual string TableName { get; }
        protected virtual string TableSchema { get; }
        protected virtual string MigrationIdColumnName
            => _migrationIdColumnName ??= EnsureModel()
                .FindEntityType(typeof(CassandraHistoryRow))
                .FindProperty(nameof(CassandraHistoryRow.MigrationId))
                .GetColumnName();
        protected virtual string ProductVersionColumnName
            => _productVersionColumnName ??= EnsureModel()
                .FindEntityType(typeof(CassandraHistoryRow))
                .FindProperty(nameof(CassandraHistoryRow.ProductVersion))
                .GetColumnName();

        protected virtual string ExistsScript => $"SELECT count(*) FROM system_schema.tables WHERE keyspace_name='{TableSchema}' and table_name='{TableName}'";
        
        private IModel EnsureModel()
        {
            if (_model == null)
            {
                var conventionSet = Dependencies.ConventionSetBuilder.CreateConventionSet();
                ConventionSet.Remove(conventionSet.ModelInitializedConventions, typeof(DbSetFindingConvention));
                var modelBuilder = new ModelBuilder(conventionSet);
                modelBuilder.Entity<CassandraHistoryRow>(
                    x =>
                    {
                        ConfigureTable(x);
                        x.ToTable(TableName, TableSchema);
                    });

                _model = modelBuilder.FinalizeModel();
            }

            return _model;
        }


        public bool Exists() => Dependencies.DatabaseCreator.Exists()
               && InterpretExistsResult(
                   Dependencies.RawSqlCommandBuilder.Build(ExistsScript).ExecuteScalar(
                       new RelationalCommandParameterObject(
                           Dependencies.Connection,
                           null,
                           null,
                           Dependencies.CurrentContext.Context,
                           Dependencies.CommandLogger)));

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default) =>
            await Dependencies.DatabaseCreator.ExistsAsync(cancellationToken)
               && InterpretExistsResult(
                   await Dependencies.RawSqlCommandBuilder.Build(ExistsScript).ExecuteScalarAsync(
                       new RelationalCommandParameterObject
                       (
                           Dependencies.Connection,
                           null,
                           null,
                            Dependencies.CurrentContext.Context,
                            Dependencies.CommandLogger
                       ), cancellationToken));

        public IReadOnlyList<HistoryRow> GetAppliedMigrations()
        {
            var rows = new List<HistoryRow>();
            if (Exists())
            {
                var command = Dependencies.RawSqlCommandBuilder.Build(GetAppliedMigrationsSql);
                using (var reader = command.ExecuteReader(
                    new RelationalCommandParameterObject(
                        Dependencies.Connection,
                        null,
                        null,
                        Dependencies.CurrentContext.Context,
                        Dependencies.CommandLogger)))
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
                using (var reader = await command.ExecuteReaderAsync(new RelationalCommandParameterObject(
                        Dependencies.Connection,
                        null,
                        null,
                        Dependencies.CurrentContext.Context,
                        Dependencies.CommandLogger
                    ), cancellationToken: cancellationToken))
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
            var operations = Dependencies.ModelDiffer.GetDifferences(null, _model);
            var commandList = Dependencies.MigrationsSqlGenerator.Generate(operations, _model);
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

// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    public class CassandraMigrationsSqlGenerator : MigrationsSqlGenerator
    {
        private static readonly
            IReadOnlyDictionary<Type, Action<CassandraMigrationsSqlGenerator, MigrationOperation, IModel, MigrationCommandListBuilder>>
            _generateActions =
                new Dictionary<Type, Action<CassandraMigrationsSqlGenerator, MigrationOperation, IModel, MigrationCommandListBuilder>>
                {
                    { typeof(AddColumnOperation), (g, o, m, b) => g.Generate((AddColumnOperation)o, m, b) },
                    { typeof(AddForeignKeyOperation), (g, o, m, b) => g.Generate((AddForeignKeyOperation)o, m, b) },
                    { typeof(AddPrimaryKeyOperation), (g, o, m, b) => g.Generate((AddPrimaryKeyOperation)o, m, b) },
                    { typeof(AddUniqueConstraintOperation), (g, o, m, b) => g.Generate((AddUniqueConstraintOperation)o, m, b) },
                    { typeof(AlterColumnOperation), (g, o, m, b) => g.Generate((AlterColumnOperation)o, m, b) },
                    { typeof(AlterDatabaseOperation), (g, o, m, b) => g.Generate((AlterDatabaseOperation)o, m, b) },
                    { typeof(AlterSequenceOperation), (g, o, m, b) => g.Generate((AlterSequenceOperation)o, m, b) },
                    { typeof(AlterTableOperation), (g, o, m, b) => g.Generate((AlterTableOperation)o, m, b) },
                    { typeof(CreateCheckConstraintOperation), (g, o, m, b) => g.Generate((CreateCheckConstraintOperation)o, m, b) },
                    { typeof(CreateIndexOperation), (g, o, m, b) => g.Generate((CreateIndexOperation)o, m, b) },
                    { typeof(CreateSequenceOperation), (g, o, m, b) => g.Generate((CreateSequenceOperation)o, m, b) },
                    { typeof(CreateTableOperation), (g, o, m, b) => g.Generate((CreateTableOperation)o, m, b) },
                    { typeof(DropColumnOperation), (g, o, m, b) => g.Generate((DropColumnOperation)o, m, b) },
                    { typeof(DropForeignKeyOperation), (g, o, m, b) => g.Generate((DropForeignKeyOperation)o, m, b) },
                    { typeof(DropIndexOperation), (g, o, m, b) => g.Generate((DropIndexOperation)o, m, b) },
                    { typeof(DropPrimaryKeyOperation), (g, o, m, b) => g.Generate((DropPrimaryKeyOperation)o, m, b) },
                    { typeof(DropSchemaOperation), (g, o, m, b) => g.Generate((DropSchemaOperation)o, m, b) },
                    { typeof(DropSequenceOperation), (g, o, m, b) => g.Generate((DropSequenceOperation)o, m, b) },
                    { typeof(DropTableOperation), (g, o, m, b) => g.Generate((DropTableOperation)o, m, b) },
                    { typeof(DropUniqueConstraintOperation), (g, o, m, b) => g.Generate((DropUniqueConstraintOperation)o, m, b) },
                    { typeof(DropCheckConstraintOperation), (g, o, m, b) => g.Generate((DropCheckConstraintOperation)o, m, b) },
                    { typeof(EnsureSchemaOperation), (g, o, m, b) => g.Generate((EnsureSchemaOperation)o, m, b) },
                    { typeof(RenameColumnOperation), (g, o, m, b) => g.Generate((RenameColumnOperation)o, m, b) },
                    { typeof(RenameIndexOperation), (g, o, m, b) => g.Generate((RenameIndexOperation)o, m, b) },
                    { typeof(RenameSequenceOperation), (g, o, m, b) => g.Generate((RenameSequenceOperation)o, m, b) },
                    { typeof(RenameTableOperation), (g, o, m, b) => g.Generate((RenameTableOperation)o, m, b) },
                    { typeof(RestartSequenceOperation), (g, o, m, b) => g.Generate((RestartSequenceOperation)o, m, b) },
                    { typeof(SqlOperation), (g, o, m, b) => g.Generate((SqlOperation)o, m, b) },
                    { typeof(InsertDataOperation), (g, o, m, b) => g.Generate((InsertDataOperation)o, m, b) },
                    { typeof(DeleteDataOperation), (g, o, m, b) => g.Generate((DeleteDataOperation)o, m, b) },
                    { typeof(UpdateDataOperation), (g, o, m, b) => g.Generate((UpdateDataOperation)o, m, b) },
                    { typeof(CreateUserDefinedTypeOperation), (g, o, m, b) => g.Generate((CreateUserDefinedTypeOperation)o, m, b) },
                    { typeof(DropUserDefinedTypeOperation), (g, o, m, b) => g.Generate((DropUserDefinedTypeOperation)o, m, b) }
                };

        public CassandraMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies) : base(dependencies) { }

        public override IReadOnlyList<MigrationCommand> Generate(IReadOnlyList<MigrationOperation> operations, IModel model = null)
        {
            var builder = new MigrationCommandListBuilder(Dependencies);
            var orderedOperations = operations.OrderBy(c => c, new MigrationOpComparer());
            foreach (var operation in orderedOperations)
            {
                Generate(operation, model, builder);
            }

            return builder.GetCommandList();
        }
        protected override void Generate(MigrationOperation operation, IModel model,MigrationCommandListBuilder builder)
        {
            var operationType = operation.GetType();
            if (!_generateActions.TryGetValue(operationType, out var generateAction))
            {
                throw new InvalidOperationException(RelationalStrings.UnknownOperation(GetType().ShortDisplayName(), operationType));
            }

            generateAction(this, operation, model, builder);
        }

        protected virtual void Generate(CreateUserDefinedTypeOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
            builder
                .Append("CREATE TYPE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .AppendLine(" (");
            using (builder.Indent())
            {
                for (var i = 0; i < operation.Columns.Count; i++)
                {
                    var column = operation.Columns[i];
                    ColumnDefinition(column, model, builder);

                    if (i != operation.Columns.Count - 1)
                    {
                        builder.AppendLine(",");
                    }
                }
                builder.AppendLine();
            }

            builder.Append(")");
            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                EndStatement(builder);
            }
        }

        protected virtual void Generate(DropUserDefinedTypeOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
            builder
                .Append("DROP TYPE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema));

            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                EndStatement(builder);
            }
        }

        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
            builder
                .Append("CREATE TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .AppendLine(" (");
            using (builder.Indent())
            {
                for (var i = 0; i < operation.Columns.Count; i++)
                {
                    var column = operation.Columns[i];
                    ColumnDefinition(column, model, builder);

                    if (i != operation.Columns.Count - 1)
                    {
                        builder.AppendLine(",");
                    }
                }                

                if (operation.PrimaryKey != null)
                {
                    builder.AppendLine(",");
                    PrimaryKeyConstraint(operation.PrimaryKey, model, builder);
                }

                builder.AppendLine();
            }

            builder.Append(")");
            var entityType = model.GetEntityTypes().First(s => s.GetTableName() == operation.Name);
            var options = entityType.GetClusteringOrderByOptions();
            if (options.Any())
            {
                builder.AppendLine().Append("WITH CLUSTERING ORDER BY (");
                var lstOpts = new List<string>();
                foreach(var option in options)
                {
                    lstOpts.Add($"{Dependencies.SqlGenerationHelper.DelimitIdentifier(option.ColumnName)} {(option.Order == CassandraClusteringOrderByOptions.ASC ? "ASC" : "DESC")}");
                }

                builder.Append(string.Join(",", lstOpts));
                builder.Append(")");
            }

            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                EndStatement(builder);
            }
        }

        protected override void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
            builder
                .Append("ALTER TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" DROP ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));

            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                EndStatement(builder);
            }
        }

        protected override void Generate(DropSchemaOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            builder
                .Append("DROP KEYSPACE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

            EndStatement(builder);
        }

        protected override void Generate(EnsureSchemaOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var keySpaceConfiguration = model.GetKeyspaceConfiguration();
            if (keySpaceConfiguration == null)
            {
                keySpaceConfiguration = new KeyspaceReplicationSimpleStrategyClass(2);
            }

            builder
                .Append("CREATE KEYSPACE IF NOT EXISTS ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));
            if (keySpaceConfiguration != null)
            {
                builder.Append("WITH REPLICATION = { ")
                    .Append("'class'")
                    .Append(" : ");
                switch(keySpaceConfiguration.ReplicationClass)
                {
                    case KeyspaceReplicationClasses.SimpleStrategy:
                        var simpleStrategy = (KeyspaceReplicationSimpleStrategyClass)keySpaceConfiguration;
                        builder.Append("'SimpleStrategy'")
                            .Append(" , ")
                            .Append("'replication_factor'")
                            .Append($" : {simpleStrategy.ReplicationFactor}")
                            .AppendLine(" } ");
                        break;
                    case KeyspaceReplicationClasses.NetworkTopologyStrategy:
                        var networkStrategy = (KeyspaceReplicationNetworkTopologyStrategyClass)keySpaceConfiguration;
                        builder.Append("'NetworkTopologyStrategy'");
                        foreach(var kvp in networkStrategy.DataCenters)
                        {
                            builder.AppendLine($" , '{kvp.Key}' : {kvp.Value}");
                        }

                        builder.Append(" } ");
                        break;
                }
            }
            
            builder.Append(Dependencies.SqlGenerationHelper.StatementTerminator);
            EndStatement(builder);
        }

        protected override void PrimaryKeyConstraint(AddPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var entityType = model.GetEntityTypes().First(s => s.GetTableName() == operation.Table);
            builder
                .Append("PRIMARY KEY ");
            var clusterColumns = entityType.GetClusterColumns();
            builder.Append("(")
                .Append("(").Append(ColumnList(operation.Columns.Except(clusterColumns).ToArray())).Append(")");
            if (clusterColumns.Any())
            {
                builder.Append(",");
                builder.Append(ColumnList(clusterColumns.ToArray()));
            }

            builder.Append(")");
        }

        protected override void ColumnDefinition(string schema, string table, string name, ColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var ut = model.FindEntityType(operation.ClrType);
            var isUserDefinedType = ut != null && ut.IsUserDefinedType();
            var columnType = operation.ColumnType ?? GetColumnType(schema, table, name, operation, model);
            if(isUserDefinedType)
            {
                columnType = $"FROZEN<{columnType}>";
            }

            var entityType = model.GetEntityTypes().First(s => s.GetTableName() == table);
            builder
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name))
                .Append(" ")
                .Append(columnType);
            if (entityType.GetStaticColumns().Contains(name))
            {
                builder.Append(" STATIC");
            }
        }

        private class MigrationOpComparer : IComparer<MigrationOperation>
        {
            public int Compare(MigrationOperation x, MigrationOperation y)
            {
                if (x is EnsureSchemaOperation)
                {
                    return -1;
                }

                if (y is CreateUserDefinedTypeOperation || y is EnsureSchemaOperation)
                {
                    return 1;
                }

                return -1;
            }
        }
    }
}
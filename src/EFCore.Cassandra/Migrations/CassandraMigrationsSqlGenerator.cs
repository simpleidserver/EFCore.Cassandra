// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    public class CassandraMigrationsSqlGenerator : MigrationsSqlGenerator
    {
        public CassandraMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies) : base(dependencies) { }

        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate)
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

        protected override void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate)
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
            var keySpaceConfiguration = model.GetKeyspace(operation.Name);
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
            var columnType = operation.ColumnType ?? GetColumnType(schema, table, name, operation, model);
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
    }
}
// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using EFCore.Cassandra.Tests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using Xunit;

namespace EFCore.Cassandra.Tests.Migrations
{
    public class MigrationCassandraGeneratorFixture
    {        
        [Fact]
        public void When_Create_Table_With_Different_Column_Types_Then_CQL_Is_Returned()
        {
            // TODO
        }

        [Fact]
        public void When_Create_Table_With_Different_Options_Then_CQL_Is_Returned()
        {
            Action<ModelBuilder> firstModelBuilderCallback = (modelBuilder) =>
            {
                var entity = modelBuilder.Entity<Applicant>()
                    .ToTable("applicants");
                entity.HasKey(p => p.Id);
                entity.ForCassandraSetClusterColumns(new[] { "lastname" });
                entity.ForCassandraSetClusteringOrderBy(new[] { new CassandraClusteringOrderByOption("lastname", CassandraClusteringOrderByOptions.ASC) });
            };
            Action<ModelBuilder> secondModelBuilderCallback = (modelBuilder) =>
            {
                var entity = modelBuilder.Entity<Applicant>()
                    .ToTable("applicants");
                entity.HasKey(p => p.Id);
                entity.ForCassandraSetClusterColumns(new[] { "lastname", "applicationname" });
                entity.ForCassandraSetClusteringOrderBy(new[] 
                {
                    new CassandraClusteringOrderByOption("lastname", CassandraClusteringOrderByOptions.ASC),
                    new CassandraClusteringOrderByOption("applicationname", CassandraClusteringOrderByOptions.DESC)
                });
            };
            var createTable = new CreateTableOperation
            {
                Name = "applicants",
                Schema = "cv",
                Columns =
                {
                    new AddColumnOperation
                    {
                        Name = "id",
                        ClrType = typeof(Guid),
                        Table = "applicants"
                    },
                    new AddColumnOperation
                    {
                        Name = "lastname",
                        ClrType = typeof(string),
                        Table = "applicants"
                    },
                    new AddColumnOperation
                    {
                        Name = "applicationname",
                        ClrType = typeof(string),
                        Table = "applicants"
                    }
                },
                PrimaryKey = new AddPrimaryKeyOperation
                {
                    Columns = new[] { "id" },
                    Table = "applicants"
                }
            };
            var firstSql = BuildSql(firstModelBuilderCallback, createTable);
            var secondSql = BuildSql(secondModelBuilderCallback, createTable);
            Assert.Equal("CREATE TABLE \"cv\".\"applicants\" (\r\n    \"id\" uuid,\r\n    \"lastname\" text,\r\n    \"applicationname\" text,\r\n    PRIMARY KEY ((\"id\"),\"lastname\")\r\n)\r\nWITH CLUSTERING ORDER BY (\"lastname\" ASC);\r\n", firstSql);
            Assert.Equal("CREATE TABLE \"cv\".\"applicants\" (\r\n    \"id\" uuid,\r\n    \"lastname\" text,\r\n    \"applicationname\" text,\r\n    PRIMARY KEY ((\"id\"),\"lastname\", \"applicationname\")\r\n)\r\nWITH CLUSTERING ORDER BY (\"lastname\" ASC,\"applicationname\" DESC);\r\n", secondSql);
        }

        [Fact]
        public void When_DropColumn_Then_CQL_Is_Returned()
        {
            var dropColumn = new DropColumnOperation
            {
                Name = "firstname",
                Schema = "cv",
                Table = "applicants"
            };
            var fakeDbContext = (IInfrastructure<IServiceProvider>)new FakeDbContext((modelBuilder) =>
            {
                modelBuilder.Entity<Applicant>()
                    .ToTable("applicants")
                    .HasKey(p => p.Id);
            });
            var migrationsSqlGenerator = (IMigrationsSqlGenerator)fakeDbContext.Instance.GetService(typeof(IMigrationsSqlGenerator));
            var sql = migrationsSqlGenerator.Generate(new[]
            {
                dropColumn
            });
            Assert.Equal("ALTER TABLE \"cv\".\"applicants\" DROP \"firstname\";\r\n", sql.First().CommandText);
        }

        [Fact]
        public void When_Drop_Table_Then_CQL_Is_Returned()
        {
            var dropTableOperation = new DropTableOperation
            {
                Name = "applicants",
                Schema = "cv"
            };
            var fakeDbContext = (IInfrastructure<IServiceProvider>)new FakeDbContext((modelBuilder) =>
            {
                modelBuilder.Entity<Applicant>()
                    .ToTable("applicants")
                    .HasKey(p => p.Id);
            });
            var migrationsSqlGenerator = (IMigrationsSqlGenerator)fakeDbContext.Instance.GetService(typeof(IMigrationsSqlGenerator));
            var sql = migrationsSqlGenerator.Generate(new[]
            {
                dropTableOperation
            });
            Assert.Equal("DROP TABLE \"cv\".\"applicants\";\r\n", sql.First().CommandText);
        }

        [Fact]
        public void When_Ensure_Schema_Operation_Then_CQL_Is_Returned()
        {
            Action<ModelBuilder> firstModelBuilderCallback = (modelBuilder) =>
            {
                modelBuilder.EnsureKeyspaceCreated(new KeyspaceReplicationSimpleStrategyClass(2));
            };
            Action<ModelBuilder> secondModelBuilderCallback = (modelBuilder) =>
            {
                modelBuilder.EnsureKeyspaceCreated(new KeyspaceReplicationNetworkTopologyStrategyClass(new Dictionary<string, int>
                {
                    { "datacenter1", 1 },
                    { "datacenter2", 2 }
                }));
            };
            var ensureSchemaOperation = new EnsureSchemaOperation
            {
                Name = "cv"
            };
            var firstSql = BuildSql(firstModelBuilderCallback, ensureSchemaOperation);
            var secondSql = BuildSql(secondModelBuilderCallback, ensureSchemaOperation);
            Assert.Equal("CREATE KEYSPACE IF NOT EXISTS \"cv\"WITH REPLICATION = { 'class' : 'SimpleStrategy' , 'replication_factor' : 2 } \r\n;", firstSql);
            Assert.Equal("CREATE KEYSPACE IF NOT EXISTS \"cv\"WITH REPLICATION = { 'class' : 'NetworkTopologyStrategy' , 'datacenter1' : 1\r\n , 'datacenter2' : 2\r\n } ;", secondSql);
        }

        [Fact]
        public void When_Drop_Schema_Then_CQL_Is_Returned()
        {
            var dropSchemaOperation = new DropSchemaOperation
            {
                Name = "cv"
            };
            var fakeDbContext = (IInfrastructure<IServiceProvider>)new FakeDbContext((modelBuilder) =>
            {
                modelBuilder.Entity<Applicant>()
                    .ToTable("applicants")
                    .HasKey(p => p.Id);
            });
            var migrationsSqlGenerator = (IMigrationsSqlGenerator)fakeDbContext.Instance.GetService(typeof(IMigrationsSqlGenerator));
            var result = migrationsSqlGenerator.Generate(new[]
            {
                dropSchemaOperation
            });
            Assert.Equal("DROP KEYSPACE \"cv\";\r\n", result.First().CommandText);
        }

        private static string BuildSql(Action<ModelBuilder> modelBuilderCallback, MigrationOperation migrationOperation)
        {
            var fakeDbContext = (IInfrastructure<IServiceProvider>)new FakeDbContext(modelBuilderCallback);
            var migrationsSqlGenerator = (IMigrationsSqlGenerator)fakeDbContext.Instance.GetService(typeof(IMigrationsSqlGenerator));
            return migrationsSqlGenerator.Generate(new[]
            {
                migrationOperation
            }, BuildModel(modelBuilderCallback)).First().CommandText;
        }

        private static IModel BuildModel(Action<ModelBuilder> callback)
        {            
            var modelBuilder = new ModelBuilder(new ConventionSet());
            callback(modelBuilder);
            return modelBuilder.Model;
        }

        private class InternalObject
        {
            public Guid Id { get; set; }
            public long Long { get; set; }
            public byte[] Blob { get; set; }
            public bool Bool { get; set; }
            public LocalDate LocalDate { get; set; }
            public decimal Decimal { get; set; }
            public double Double { get; set; }
            public float Float { get; set; }
            public IPAddress Ip { get; set; }
            public int Integer { get; set; }
            public List<string> List { get; set; }
            public short SmallInt { get; set; }
            public LocalTime LocalTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public TimeUuid TimeUuid { get; set; }
            public sbyte Sbyte { get; set; }
            public BigInteger BigInteger { get; set; }
        }
    }
}

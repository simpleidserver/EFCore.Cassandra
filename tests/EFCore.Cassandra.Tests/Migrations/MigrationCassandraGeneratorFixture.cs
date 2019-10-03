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
        public void When_Create_Table_With_Different_KeyTypes_Then_CQL_Is_Returned()
        {
            Action<ModelBuilder> firstModelBuilderCallback = (modelBuilder) =>
            {
                var entity = modelBuilder.Entity<Applicant>()
                    .ToTable("applicants");
                entity.HasKey(p => p.Id);
            };
            Action<ModelBuilder> secondModelBuilderCallback = (modelBuilder) =>
            {
                var entity = modelBuilder.Entity<Applicant>()
                    .ToTable("applicants");
                entity.HasKey(p => p.Id);
                entity.ForCassandraSetClusterColumns(new[] { "lastname" });
            };
            Action<ModelBuilder> thirdModelBuilderCallback = (modelBuilder) =>
            {
                var entity = modelBuilder.Entity<Applicant>()
                    .ToTable("applicants");
                entity.HasKey(p => p.Id);
                entity.ForCassandraSetClusterColumns(new[] { "lastname" });
                entity.ForCassandraSetStaticColumns(new[] { "applicationname" });
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
                    },
                    new AddColumnOperation
                    {
                        Name = "lst",
                        ClrType = typeof(List<string>),
                        Table = "applicants"
                    },
                    new AddColumnOperation
                    {
                        Name = "dic",
                        ClrType = typeof(Dictionary<string, string>),
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
            var thirdSql = BuildSql(thirdModelBuilderCallback, createTable);
            Assert.Equal("CREATE TABLE \"cv\".\"applicants\" (\r\n    \"id\" uuid,\r\n    \"lastname\" text,\r\n    \"applicationname\" text,\r\n    \"lst\" list<text>,\r\n    \"dic\" map<text,text>,\r\n    PRIMARY KEY ((\"id\"))\r\n);\r\n", firstSql);
            Assert.Equal("CREATE TABLE \"cv\".\"applicants\" (\r\n    \"id\" uuid,\r\n    \"lastname\" text,\r\n    \"applicationname\" text,\r\n    \"lst\" list<text>,\r\n    \"dic\" map<text,text>,\r\n    PRIMARY KEY ((\"id\"),\"lastname\")\r\n);\r\n", secondSql);
            Assert.Equal("CREATE TABLE \"cv\".\"applicants\" (\r\n    \"id\" uuid,\r\n    \"lastname\" text,\r\n    \"applicationname\" text STATIC,\r\n    \"lst\" list<text>,\r\n    \"dic\" map<text,text>,\r\n    PRIMARY KEY ((\"id\"),\"lastname\")\r\n);\r\n", thirdSql);
        }
        
        [Fact]
        public void When_Create_Table_With_Different_Column_Types_Then_CQL_Is_Returned()
        {
            // A FAIRE
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
        public void When_AddColumn_Then_CQL_Is_Returned()
        {
            Action<ModelBuilder> firstModelBuilderCallback = (modelBuilder) =>
            {
                modelBuilder.Entity<InternalObject>()
                    .ToTable("internalobjects")
                    .HasKey(p => p.Id);
            };
            var addGuidColumn = new AddColumnOperation
            {
                Name = "Id",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(Guid)
            };
            var addLongColum = new AddColumnOperation
            {
                Name = "Long",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(long)
            };
            var addBlobColumn = new AddColumnOperation
            {
                Name = "Blob",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(byte[])
            };
            var addBoolColumn = new AddColumnOperation
            {
                Name = "Bool",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(bool)
            };
            var addLocalDateColumn = new AddColumnOperation
            {
                Name = "LocalDate",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(LocalDate)
            };
            var addDecimalColumn = new AddColumnOperation
            {
                Name = "Decimal",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(decimal)
            };
            var addDoubleColumn = new AddColumnOperation
            {
                Name = "Double",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(double)
            };
            var addFloatColumn = new AddColumnOperation
            {
                Name = "Float",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(float)
            };
            var addIpColumn = new AddColumnOperation
            {
                Name = "Ip",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(IPAddress)
            };
            var addIntegerColumn = new AddColumnOperation
            {
                Name = "Integer",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(int)
            };
            var addListColumn = new AddColumnOperation
            {
                Name = "List",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(List<string>)
            };
            var addSmallIntColumn = new AddColumnOperation
            {
                Name = "SmallInt",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(short)
            };
            var addLocalTimeColumn = new AddColumnOperation
            {
                Name = "LocalTime",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(LocalTime)
            };
            var addDateTimeOffsetColumn = new AddColumnOperation
            {
                Name = "DateTimeOffset",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(DateTimeOffset)
            };
            var addTimeUuidColumn = new AddColumnOperation
            {
                Name = "TimeUuid",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(TimeUuid)
            };
            var addSbyteColumn = new AddColumnOperation
            {
                Name = "Sbyte",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(SByte)
            };
            var addBigIntegerColumn = new AddColumnOperation
            {
                Name = "BigInteger",
                Schema = "cv",
                Table = "internalobjects",
                ClrType = typeof(BigInteger)
            };
            var fakeDbContext = (IInfrastructure<IServiceProvider>)new FakeDbContext(firstModelBuilderCallback);
            var migrationsSqlGenerator = (IMigrationsSqlGenerator)fakeDbContext.Instance.GetService(typeof(IMigrationsSqlGenerator));
            var firstSql = BuildSql(firstModelBuilderCallback, addGuidColumn);
            var secondSql = BuildSql(firstModelBuilderCallback, addLongColum);
            var thirdSql = BuildSql(firstModelBuilderCallback, addBlobColumn);
            var fourthSql = BuildSql(firstModelBuilderCallback, addBoolColumn);
            var fifthSql = BuildSql(firstModelBuilderCallback, addLocalDateColumn);
            var sixSql = BuildSql(firstModelBuilderCallback, addDecimalColumn);
            var sevenSql = BuildSql(firstModelBuilderCallback, addDoubleColumn);
            var eightSql = BuildSql(firstModelBuilderCallback, addFloatColumn);
            var nineSql = BuildSql(firstModelBuilderCallback, addIpColumn);
            var tenSql = BuildSql(firstModelBuilderCallback, addIntegerColumn);
            var elevenSql = BuildSql(firstModelBuilderCallback, addListColumn);
            var twelveSql = BuildSql(firstModelBuilderCallback, addSmallIntColumn);
            var thirteenSql = BuildSql(firstModelBuilderCallback, addLocalTimeColumn);
            var fourteenSql = BuildSql(firstModelBuilderCallback, addDateTimeOffsetColumn);
            var fifteenSql = BuildSql(firstModelBuilderCallback, addTimeUuidColumn);
            var sixteenSql = BuildSql(firstModelBuilderCallback, addSbyteColumn);
            var seventeenSql = BuildSql(firstModelBuilderCallback, addBigIntegerColumn);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Id\" uuid;\r\n", firstSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Long\" bigint;\r\n", secondSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Blob\" blob;\r\n", thirdSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Bool\" boolean;\r\n", fourthSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"LocalDate\" date;\r\n", fifthSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Decimal\" decimal;\r\n", sixSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Double\" double;\r\n", sevenSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Float\" float;\r\n", eightSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Ip\" inet;\r\n", nineSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Integer\" int;\r\n", tenSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"List\" list<text>;\r\n", elevenSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"SmallInt\" smallint;\r\n", twelveSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"LocalTime\" time;\r\n", thirteenSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"DateTimeOffset\" timestamp;\r\n", fourteenSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"TimeUuid\" timeuuid;\r\n", fifteenSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"Sbyte\" tinyint;\r\n", sixteenSql);
            Assert.Equal("ALTER TABLE \"cv\".\"internalobjects\" ADD \"BigInteger\" varint;\r\n", seventeenSql);
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
                modelBuilder.ForCassandraAddKeyspace("cv", new KeyspaceReplicationSimpleStrategyClass(2));
            };
            Action<ModelBuilder> secondModelBuilderCallback = (modelBuilder) =>
            {
                modelBuilder.ForCassandraAddKeyspace("cv", new KeyspaceReplicationNetworkTopologyStrategyClass(new Dictionary<string, int>
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
            var s = fakeDbContext.Instance.GetService(typeof(InternalModelBuilder));
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

﻿// <auto-generated />
using Cassandra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;

namespace EFCore.Cassandra.Samples.Migrations
{
    [DbContext(typeof(FakeDbContext))]
    [Migration("20191003081406_CV")]
    partial class CV
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Cassandra:Keyspacecv", "{\"ReplicationFactor\":2,\"ReplicationClass\":0}")
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("EFCore.Cassandra.Integration.Tests.Models.Applicant", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnName("id");

                    b.Property<string>("LastName");

                    b.Property<BigInteger>("BigInteger");

                    b.Property<byte[]>("Blob");

                    b.Property<bool>("Bool");

                    b.Property<DateTimeOffset>("DateTimeOffset");

                    b.Property<decimal>("Decimal");

                    b.Property<IDictionary<string, string>>("Dic");

                    b.Property<double>("Double");

                    b.Property<float>("Float");

                    b.Property<int>("Integer");

                    b.Property<IPAddress>("Ip");

                    b.Property<LocalDate>("LocalDate");

                    b.Property<LocalTime>("LocalTime");

                    b.Property<long>("Long");

                    b.Property<IList<string>>("Lst");

                    b.Property<IList<int>>("LstInt");

                    b.Property<sbyte>("Sbyte");

                    b.Property<short>("SmallInt");

                    b.Property<Guid>("TimeUuid")
                        .HasConversion(new ValueConverter<Guid, Guid>(v => default(Guid), v => default(Guid), new ConverterMappingHints(size: 36)));

                    b.HasKey("Id", "LastName");

                    b.ToTable("applicants","cv");

                    b.HasAnnotation("Cassandra:ClusterColumns", new[] { "LastName" });
                });

            modelBuilder.Entity("EFCore.Cassandra.Integration.Tests.Models.CV", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.HasKey("Id");

                    b.ToTable("cvs","cv");
                });
#pragma warning restore 612, 618
        }
    }
}

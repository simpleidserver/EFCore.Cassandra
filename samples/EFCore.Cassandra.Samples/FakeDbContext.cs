// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using EFCore.Cassandra.Samples.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cassandra.Storage;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EFCore.Cassandra.Samples
{
    public class FakeDbContext : DbContext
    {
        private const string SCHEMA_NAME = "cv";
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<User> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ConfigureDataxAstra(optionsBuilder);
            ConfigureLocalDB(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var timeUuidConverter = new TimeUuidToGuidConverter();
            modelBuilder.EnsureKeyspaceCreated(new KeyspaceReplicationSimpleStrategyClass(2));
            // Il ne faut pas passer le schema dans "ToTable"
            modelBuilder.Entity<Applicant>()
                .ToTable("applicants", SCHEMA_NAME)
                .HasKey(p => new { p.Id, p.Order });
            /*
            modelBuilder.Entity<Applicant>()
                .Property(p => p.Phones)
                .HasColumnType("set<frozen<applicant_addr>>");
            */
            modelBuilder.Entity<Applicant>()
                .ForCassandraSetClusterColumns(_ => _.Order)
                .ForCassandraSetClusteringOrderBy(new[] { new CassandraClusteringOrderByOption("Order", CassandraClusteringOrderByOptions.ASC) });
            modelBuilder.Entity<Applicant>()
               .Property(p => p.TimeUuid)
               .HasConversion(new TimeUuidToGuidConverter());
            modelBuilder.Entity<Applicant>()
                .Property(p => p.Email).HasColumnName("email");
            modelBuilder.Entity<ApplicantPhone>()
                .ToUserDefinedType("applicant_phone", SCHEMA_NAME)
                .HasNoKey();
            modelBuilder.Entity<ApplicantAddress>()
                .ToUserDefinedType("applicant_addr", SCHEMA_NAME)
                .HasNoKey();
            modelBuilder.Entity<User>()
                .ToTable("users", SCHEMA_NAME)
                .HasKey(u => u.Email);
            modelBuilder.Entity<User>()
                .Property(u => u.Id).HasColumnName("id");
            modelBuilder.Entity<User>()
                .Property(u => u.Email).HasColumnName("email");
            modelBuilder.Entity<User>()
                .ForCassandraSetClusterColumns(_ => _.Email);
            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureDataxAstra(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSecuredCassandra(SCHEMA_NAME, opt =>
            {
                opt.MigrationsHistoryTable(HistoryRepository.DefaultTableName);
            }, o => {
                o.WithCloudSecureConnectionBundle(@"c:\Projects\EFCore.Cassandra\secure-connect-simpleidserver.zip")
                    .WithCredentials("OyLmLXBhXQgmyxOOmEIlBgyq", "+L7SMKvoH1_A97CjYXK4xx7wl96d37HnT9Wg-n426MjTN0H,q4TqR2goC170p7MiSP8QKTMFxXUR7Lo9QAXy9YEBDdD++jHSZnd5qEwm_zT80xl1_uo0kO_PXoswFL+K");
            });
        }

        private void ConfigureLocalDB(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseCassandra("Contact Points=127.0.0.1;Username=cassandra;Password=password", SCHEMA_NAME, opt =>
            {
                opt.MigrationsHistoryTable(HistoryRepository.DefaultTableName);
            }, o => {

                o.WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalOne))
                    .WithReconnectionPolicy(new ConstantReconnectionPolicy(1000))
                    .WithRetryPolicy(new DefaultRetryPolicy())
                    .WithLoadBalancingPolicy(new TokenAwarePolicy(Policies.DefaultPolicies.LoadBalancingPolicy))
                    .WithDefaultKeyspace(GetType().Name)
                    .WithPoolingOptions(
                    PoolingOptions.Create()
                        .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Remote, 1_000_000)
                        .SetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance.Local, 1_000_000)
                        .SetMaxConnectionsPerHost(HostDistance.Local, 1_000_000)
                        .SetMaxConnectionsPerHost(HostDistance.Remote, 1_000_000)
                        .SetMaxRequestsPerConnection(1_000_000)
                );
            });
        }
    }
}
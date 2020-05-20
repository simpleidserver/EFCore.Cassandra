// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using EFCore.Cassandra.Tests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq;
using Xunit;

namespace EFCore.Cassandra.Tests.Metadata
{
    public class CassandraBuilderExtensionsFixture
    {
        [Fact]
        public void Can_Set_ClusterColumns()
        {
            var modelBuilder = new ModelBuilder(new ConventionSet());
            modelBuilder.Entity<Cv>().ForCassandraSetClusterColumns(new[] { "Id" });

            var entityType = modelBuilder.Model.FindEntityType(typeof(Cv));
            var clusterColumns = entityType.GetClusterColumns();

            Assert.NotNull(clusterColumns);
            Assert.True(clusterColumns.Count() == 1);
            Assert.Equal("Id", clusterColumns.First());
        }

        [Fact]
        public void Can_Set_StaticColumns()
        {
            var modelBuilder = new ModelBuilder(new ConventionSet());
            modelBuilder.Entity<Cv>().ForCassandraSetStaticColumns(new[] { "Id" });

            var entityType = modelBuilder.Model.FindEntityType(typeof(Cv));
            var staticColumns = entityType.GetStaticColumns();

            Assert.NotNull(staticColumns);
            Assert.True(staticColumns.Count() == 1);
            Assert.Equal("Id", staticColumns.First());
        }
    }
}

// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraModelTypeBuilderExtensions
    {
        public static ModelBuilder ForCassandraAddKeyspace(this ModelBuilder modelBuilder, string keyspaceName, KeyspaceReplicationSimpleStrategyClass configuration)
        {
            modelBuilder.GetInfrastructure().Metadata.Cassandra().SetKeyspace(keyspaceName, configuration);
            return modelBuilder;
        }

        public static ModelBuilder ForCassandraAddKeyspace(this ModelBuilder modelBuilder, string keyspaceName, KeyspaceReplicationNetworkTopologyStrategyClass configuration)
        {
            modelBuilder.GetInfrastructure().Metadata.Cassandra().SetKeyspace(keyspaceName, configuration);
            return modelBuilder;
        }
    }
}

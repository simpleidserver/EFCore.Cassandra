// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraModelBuilderExtensions
    {
        public static ModelBuilder ForCassandraAddKeyspace(this ModelBuilder modelBuilder, string keyspaceName, KeyspaceReplicationConfiguration configuration)
        {
            var model = modelBuilder.Model;
            model.SetKeyspace(keyspaceName, configuration);
            return modelBuilder;
        }
    }
}

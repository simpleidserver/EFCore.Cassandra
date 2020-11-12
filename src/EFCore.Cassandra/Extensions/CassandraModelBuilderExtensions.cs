// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraModelBuilderExtensions
    {
        public static ModelBuilder EnsureKeyspaceCreated(this ModelBuilder modelBuilder, KeyspaceReplicationConfiguration configuration)
        {
            var model = modelBuilder.Model;
            model.SetKeyspaceConfiguration(configuration);
            return modelBuilder;
        }
    }
}

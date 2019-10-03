// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Metadata;

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraMetadataExtensions
    {
        public static ICassandraEntityTypeAnnotations Cassandra(this IEntityType entityType)
        {
            return new CassandraEntityTypeAnnotations(entityType);
        }

        public static ICassandraEntityTypeAnnotations Cassandra(this IMutableEntityType entityType)
        {
            return new CassandraEntityTypeAnnotations(entityType);
        }

        public static ICassandraModelTypeAnnotations Cassandra(this IModel modelType)
        {
            return new CassandraModelTypeAnnotations(modelType);
        }
    }
}
// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraEntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder ForCassandraSetClusterColumns<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, Expression<Func<TEntity, object>> keyExpression) where TEntity : class
        {
            entityTypeBuilder.Metadata.Cassandra().ClusterColumns = keyExpression.GetPropertyAccessList().Select(s => s.Name).ToArray();
            return entityTypeBuilder;
        }

        public static EntityTypeBuilder ForCassandraSetClusterColumns(this EntityTypeBuilder entityTypeBuilder, IEnumerable<string> columnNames)
        {
            entityTypeBuilder.Metadata.Cassandra().ClusterColumns = columnNames;
            return entityTypeBuilder;
        }

        public static EntityTypeBuilder ForCassandraSetStaticColumns(this EntityTypeBuilder entityTypeBuilder, IEnumerable<string> columnNames)
        {
            entityTypeBuilder.Metadata.Cassandra().StaticColumns = columnNames;
            return entityTypeBuilder;
        }

        public static EntityTypeBuilder ForCassandraSetClusteringOrderBy(this EntityTypeBuilder entityTypeBuilder, IEnumerable<CassandraClusteringOrderByOption> options)
        {
            entityTypeBuilder.Metadata.Cassandra().ClusteringOrderByOptions = options;
            return entityTypeBuilder;
        }
    }
}

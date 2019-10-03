// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Metadata.Internal;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    public class CassandraEntityTypeAnnotations : RelationalEntityTypeAnnotations, ICassandraEntityTypeAnnotations
    {
        public CassandraEntityTypeAnnotations(IEntityType entityType) : base(entityType)
        {
        }

        public virtual IEnumerable<string> ClusterColumns
        {
            get => Annotations.Metadata[CassandraAnnotationNames.ClusterColumns] as IEnumerable<string> ?? new string[0];
            set => SetClusterColumns(value);
        }

        public virtual IEnumerable<string> StaticColumns
        {
            get => Annotations.Metadata[CassandraAnnotationNames.StaticColumns] as IEnumerable<string> ?? new string[0];
            set => SetStaticColumns(value);
        }

        public IEnumerable<CassandraClusteringOrderByOption> ClusteringOrderByOptions
        {
            get => Annotations.Metadata[CassandraAnnotationNames.ClusteringOrderByOptions] as IEnumerable<CassandraClusteringOrderByOption> ?? new CassandraClusteringOrderByOption[0];
            set => SetClusteringOrderByOptions(value);
        }

        protected virtual void SetClusterColumns(IEnumerable<string> keys) => Annotations.SetAnnotation(CassandraAnnotationNames.ClusterColumns, keys);

        protected virtual void SetStaticColumns(IEnumerable<string> keys) => Annotations.SetAnnotation(CassandraAnnotationNames.StaticColumns, keys);

        protected virtual void SetClusteringOrderByOptions(IEnumerable<CassandraClusteringOrderByOption> options) => Annotations.SetAnnotation(CassandraAnnotationNames.ClusteringOrderByOptions, options);

        protected virtual void SetKeyspaces(IEnumerable<CassandraClusteringOrderByOption> options) => Annotations.SetAnnotation(CassandraAnnotationNames.Keyspace, options);
    }
}

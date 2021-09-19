// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraEntityTypeExtensions
    {
        public static IEnumerable<string> GetClusterColumns(this IEntityType model)
        {
            var result = model.FindAnnotation(CassandraAnnotationNames.ClusterColumns);
            if (result == null)
            {
                return new string[0];
            }

            return (IEnumerable<string>)result.Value;
        }

        public static IEnumerable<string> GetStaticColumns(this IEntityType model)
        {
            var result = model.FindAnnotation(CassandraAnnotationNames.StaticColumns);
            if (result == null)
            {
                return new string[0];
            }

            return (IEnumerable<string>)result.Value;
        }

        public static IEnumerable<CassandraClusteringOrderByOption> GetClusteringOrderByOptions(this IEntityType model)
        {
            var result = model.FindAnnotation(CassandraAnnotationNames.ClusteringOrderByOptions);
            if (result == null)
            {
                return new CassandraClusteringOrderByOption[0];
            }

            return JsonConvert.DeserializeObject<IEnumerable<CassandraClusteringOrderByOption>>(result.Value.ToString());
        }

        public static bool IsUserDefinedType(this IEntityType model)
        {
            var result = model.FindAnnotation(CassandraAnnotationNames.IsUserDefinedType);
            if (result == null)
            {
                return false;
            }

            return (bool)result.Value;
        }

        public static void SetClusterColumns(this IMutableEntityType entityType, IEnumerable<string> clusterColumns)
        {
            entityType.SetOrRemoveAnnotation(CassandraAnnotationNames.ClusterColumns, clusterColumns);
        }

        public static void SetStaticColumns(this IMutableEntityType entityType, IEnumerable<string> statisticColumns)
        {
            entityType.SetOrRemoveAnnotation(CassandraAnnotationNames.StaticColumns, statisticColumns);
        }

        public static void SetClusteringOrderByOptions(this IMutableEntityType entityType, IEnumerable<CassandraClusteringOrderByOption> options)
        {
            var json = JsonConvert.SerializeObject(options);
            entityType.SetOrRemoveAnnotation(CassandraAnnotationNames.ClusteringOrderByOptions, json);
        }

        public static void SetIsUserDefinedType(this IMutableEntityType entityType)
        {
            entityType.SetOrRemoveAnnotation(CassandraAnnotationNames.IsUserDefinedType, true);
        }
    }
}

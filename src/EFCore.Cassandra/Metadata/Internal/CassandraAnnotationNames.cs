// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Microsoft.EntityFrameworkCore.Cassandra.Metadata.Internal
{
    public static class CassandraAnnotationNames
    {
        public const string Prefix = "Cassandra:";
        public const string PartitionColumns = Prefix + "PartitionColumns";
        public const string ClusterColumns = Prefix + "ClusterColumns";
        public const string StaticColumns = Prefix + "StaticColumns";
        public const string ClusteringOrderByOptions = Prefix + "ClusteringOrderByOptions";
        public const string IsUserDefinedType = Prefix + "IsUserDefinedType";
        public const string KeyspaceConfiguration = Prefix + "KeyspaceConfiguration";
    }
}

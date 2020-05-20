// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraModelExtensions
    {
        public static KeyspaceReplicationConfiguration GetKeyspace(this IModel model, string name)
        {
            var annotation = model.FindAnnotation($"{CassandraAnnotationNames.Keyspace}{name}");
            if (annotation == null)
            {
                return null;
            }

            var json = annotation.Value.ToString();
            var result = JsonConvert.DeserializeObject<KeyspaceReplicationConfiguration>(json);
            switch (result.ReplicationClass)
            {
                case KeyspaceReplicationClasses.NetworkTopologyStrategy:
                    return JsonConvert.DeserializeObject<KeyspaceReplicationNetworkTopologyStrategyClass>(json);
                default:
                    return JsonConvert.DeserializeObject<KeyspaceReplicationSimpleStrategyClass>(json);
            }
        }

        public static void SetKeyspace(this IMutableModel model, string name, KeyspaceReplicationConfiguration keyspaceReplicationConfiguration)
        {
            model.SetOrRemoveAnnotation($"{CassandraAnnotationNames.Keyspace}{name}", JsonConvert.SerializeObject(keyspaceReplicationConfiguration));
        }
    }
}

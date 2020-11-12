// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;

namespace Microsoft.EntityFrameworkCore
{
    public static class CassandraModelExtensions
    {
        public static KeyspaceReplicationConfiguration GetKeyspaceConfiguration(this IModel model)
        {
            var annotation = model.FindAnnotation(CassandraAnnotationNames.KeyspaceConfiguration);
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

        public static void SetKeyspaceConfiguration(this IMutableModel model, KeyspaceReplicationConfiguration keyspaceReplicationConfiguration)
        {
            model.SetOrRemoveAnnotation(CassandraAnnotationNames.KeyspaceConfiguration, JsonConvert.SerializeObject(keyspaceReplicationConfiguration));
        }
    }
}

// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Metadata.Internal;
using Newtonsoft.Json;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    public class CassandraModelTypeAnnotations : RelationalModelAnnotations, ICassandraModelTypeAnnotations
    {
        public CassandraModelTypeAnnotations(IModel modelType) : base(modelType) { }

        public KeyspaceReplicationConfiguration GetKeyspace(string name)
        {
            var json = Annotations.Metadata[$"{CassandraAnnotationNames.Keyspace}{name}"] as string ?? null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<KeyspaceReplicationConfiguration>(json);
            switch(result.ReplicationClass)
            {
                case KeyspaceReplicationClasses.NetworkTopologyStrategy:
                    return JsonConvert.DeserializeObject<KeyspaceReplicationNetworkTopologyStrategyClass>(json);
                default:
                    return JsonConvert.DeserializeObject<KeyspaceReplicationSimpleStrategyClass>(json);
            }
        }

        public void SetKeyspace(string name, KeyspaceReplicationConfiguration keyspaceReplicationConfiguration)
        {
            Annotations.SetAnnotation($"{CassandraAnnotationNames.Keyspace}{name}", JsonConvert. SerializeObject(keyspaceReplicationConfiguration));
        }
    }
}
// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Microsoft.EntityFrameworkCore
{
    public class KeyspaceReplicationSimpleStrategyClass : KeyspaceReplicationConfiguration
    {
        public KeyspaceReplicationSimpleStrategyClass(int replicationFactor) : base(KeyspaceReplicationClasses.SimpleStrategy)
        {
            ReplicationFactor = replicationFactor;
        }

        public int ReplicationFactor { get; }
    }
}
// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Microsoft.EntityFrameworkCore
{
    public class KeyspaceReplicationConfiguration
    {
        public KeyspaceReplicationConfiguration(KeyspaceReplicationClasses replicationClass)
        {
            ReplicationClass = replicationClass;
        }

        public KeyspaceReplicationClasses ReplicationClass { get; }
    }
}

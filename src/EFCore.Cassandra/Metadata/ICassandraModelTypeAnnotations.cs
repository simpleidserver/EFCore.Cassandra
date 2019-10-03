// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Microsoft.EntityFrameworkCore.Metadata
{
    public interface ICassandraModelTypeAnnotations
    {
        KeyspaceReplicationConfiguration GetKeyspace(string name);
        void SetKeyspace(string name, KeyspaceReplicationConfiguration keyspaceReplicationConfiguration);
    }
}
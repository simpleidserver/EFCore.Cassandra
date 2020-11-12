// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;
using Builder = Cassandra.Builder;

namespace EFCore.Cassandra
{
    public class CassandraOptions
    {
        public string DefaultKeyspace { get; set; }
        public Action<Builder> ClusterBuilder { get; set; }
    }
}

// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore
{
    public class KeyspaceReplicationNetworkTopologyStrategyClass : KeyspaceReplicationConfiguration
    {
        public KeyspaceReplicationNetworkTopologyStrategyClass(IDictionary<string, int> dataCenters) : base(KeyspaceReplicationClasses.NetworkTopologyStrategy)
        {
            DataCenters = dataCenters;
        }

        public IDictionary<string, int> DataCenters { get; }
    }
}

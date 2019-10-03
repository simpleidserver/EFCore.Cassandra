// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    public interface ICassandraEntityTypeAnnotations
    {
        IEnumerable<string> ClusterColumns { get; set; }
        IEnumerable<string> StaticColumns { get; set; }
        IEnumerable<CassandraClusteringOrderByOption> ClusteringOrderByOptions { get; set; }
    }
}
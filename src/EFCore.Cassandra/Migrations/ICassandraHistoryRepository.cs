// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.Cassandra.Migrations
{
    public interface ICassandraHistoryRepository : IHistoryRepository
    {
        IEnumerable<string> GetCreateScripts();
    }
}

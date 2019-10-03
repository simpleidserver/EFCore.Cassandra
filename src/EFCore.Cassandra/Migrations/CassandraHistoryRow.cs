// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Microsoft.EntityFrameworkCore.Cassandra.Migrations
{
    public class CassandraHistoryRow : HistoryRow
    {
        public CassandraHistoryRow(string migrationId, string productVersion) : base(migrationId, productVersion)
        {
        }
    }
}

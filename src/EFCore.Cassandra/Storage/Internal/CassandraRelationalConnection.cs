// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using EFCore.Cassandra.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraRelationalConnection : RelationalConnection, ICassandraRelationalConnection
    {
        public CassandraRelationalConnection(RelationalConnectionDependencies dependencies) : base(dependencies) { }

        protected override DbConnection CreateDbConnection() => new EFCassandraDbConnection(ConnectionString, Dependencies);
    }
}

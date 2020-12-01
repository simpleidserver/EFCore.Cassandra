// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using EFCore.Cassandra.Storage.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraRelationalConnection : RelationalConnection, ICassandraRelationalConnection
    {
        private readonly ICurrentDbContext _currentDbContext;

        public CassandraRelationalConnection(ICurrentDbContext currentDbContext, RelationalConnectionDependencies dependencies) : base(dependencies) 
        {
            _currentDbContext = currentDbContext;
        }

        protected override DbConnection CreateDbConnection()
        {
            var result = new EFCassandraDbConnection(_currentDbContext, ConnectionString, Dependencies);
            return result;
        }
    }
}

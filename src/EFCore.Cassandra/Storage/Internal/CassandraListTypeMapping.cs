// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Data;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraListTypeMapping<T> : RelationalTypeMapping
    {
        public CassandraListTypeMapping(string storeType, DbType? dbType = null) : base(storeType, typeof(IEnumerable<T>), dbType)
        {
        }

        protected CassandraListTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraListTypeMapping<T>(parameters);
    }
}
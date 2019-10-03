// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Data;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraDicTypeMapping<T, Y> : RelationalTypeMapping
    {
        public CassandraDicTypeMapping(string storeType, DbType? dbType = null) : base(storeType, typeof(IDictionary<T, Y>), dbType)
        {
        }

        protected CassandraDicTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraDicTypeMapping<T, Y>(parameters);
    }
}
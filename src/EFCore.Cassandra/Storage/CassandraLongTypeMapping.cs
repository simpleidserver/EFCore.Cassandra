// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class CassandraLongTypeMapping: BaseCassandraTypeMapping
    {
        public CassandraLongTypeMapping(string storeType)
            : base(storeType, typeof(long), System.Data.DbType.Int64)
        {
        }

        protected CassandraLongTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraLongTypeMapping(parameters);
    }
}

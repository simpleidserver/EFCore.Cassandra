// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class CassandraIntTypeMapping: BaseCassandraTypeMapping
    {
        public CassandraIntTypeMapping(string storeType)
            : base(storeType, typeof(int), System.Data.DbType.Int32)
        {
        }

        protected CassandraIntTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraIntTypeMapping(parameters);
    }
}

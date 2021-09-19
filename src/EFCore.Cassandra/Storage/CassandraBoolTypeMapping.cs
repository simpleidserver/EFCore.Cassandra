// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class CassandraBoolTypeMapping: BaseCassandraTypeMapping
    {
        public CassandraBoolTypeMapping(string storeType)
            : base(storeType, typeof(bool), System.Data.DbType.Boolean)
        {
        }

        protected CassandraBoolTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraBoolTypeMapping(parameters);
    }
}

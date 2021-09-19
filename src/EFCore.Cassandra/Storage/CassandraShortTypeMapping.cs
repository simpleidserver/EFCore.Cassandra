// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class CassandraShortTypeMapping : BaseCassandraTypeMapping
    {
        public CassandraShortTypeMapping(string storeType)
            : base(storeType, typeof(short), System.Data.DbType.Int16)
        {
        }

        protected CassandraShortTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraShortTypeMapping(parameters);
    }
}

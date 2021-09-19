// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class CassandraDoubleTypeMapping : BaseCassandraTypeMapping
    {
        public CassandraDoubleTypeMapping(string storeType)
            : base(storeType, typeof(double), System.Data.DbType.Double)
        {
        }

        protected CassandraDoubleTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraDoubleTypeMapping(parameters);
    }
}

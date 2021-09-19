// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class CassandraFloatTypeMapping : BaseCassandraTypeMapping
    {
        public CassandraFloatTypeMapping(string storeType)
            : base(storeType, typeof(float), System.Data.DbType.Single)
        {
        }

        protected CassandraFloatTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraFloatTypeMapping(parameters);

        protected override string GenerateNonNullSqlLiteral(object value)
            => Convert.ToSingle(value).ToString("R", CultureInfo.InvariantCulture);
    }
}

// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Storage
{
    public class CassandraDecimalTypeMapping : BaseCassandraTypeMapping
    {
        private const string DecimalFormatConst = "{0:0.0###########################}";

        public CassandraDecimalTypeMapping(string storeType)
            : base(storeType, typeof(decimal), System.Data.DbType.Decimal)
        {
        }

        protected CassandraDecimalTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraDecimalTypeMapping(parameters);

        protected override string SqlLiteralFormatString
            => DecimalFormatConst;
    }
}

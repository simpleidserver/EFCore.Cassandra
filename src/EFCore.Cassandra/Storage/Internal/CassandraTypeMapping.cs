// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraTypeMapping<T> : RelationalTypeMapping
    {
        private readonly object _defaultValue;

        public CassandraTypeMapping(string storeType, object defaultValue, DbType? dbType = null) : base(storeType, typeof(T), dbType)
        {
            _defaultValue = defaultValue;
        }

        protected CassandraTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        public override DbParameter CreateParameter(DbCommand command, string name, object value, bool? nullable = null)
        {
            var result = base.CreateParameter(command, name, value, nullable);
            if (result.Value == null || result.Value.Equals(default(T)) || string.IsNullOrWhiteSpace(result.Value.ToString()))
            {
                result.Value = _defaultValue;
            }

            return result;
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new CassandraTypeMapping<T>(parameters);

        protected virtual string EscapeSqlLiteral(string literal)
            => literal.Replace("'", "''");

        /// <summary>
        ///     Generates the SQL representation of a literal value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <returns>
        ///     The generated string.
        /// </returns>
        protected override string GenerateNonNullSqlLiteral(object value)
            => $"'{EscapeSqlLiteral((string)value)}'";
    }
}

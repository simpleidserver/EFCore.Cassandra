// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;
using System.Data;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.Storage
{
    public abstract class BaseCassandraTypeMapping : RelationalTypeMapping
    {
        public BaseCassandraTypeMapping(
            string storeType,
            Type clrType,
            DbType? dbType)
            : base(storeType, clrType, dbType)
        {
        }

        protected BaseCassandraTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }

        public override DbParameter CreateParameter(DbCommand command, string name, object value, bool? nullable = null)
        {
            var parameter = command.CreateParameter();
            parameter.Direction = ParameterDirection.Input;
            parameter.ParameterName = name;
            value = ConvertUnderlyingEnumValueToEnum(value);
            if (Converter != null)
            {
                value = Converter.ConvertToProvider(value);
            }

            parameter.Value = value ?? null;

            if (nullable.HasValue)
            {
                parameter.IsNullable = nullable.Value;
            }

            if (DbType.HasValue)
            {
                parameter.DbType = DbType.Value;
            }

            ConfigureParameter(parameter);

            return parameter;
        }

        private object? ConvertUnderlyingEnumValueToEnum(object? value)
            => value?.GetType().IsInteger() == true && ClrType.UnwrapNullableType().IsEnum
                ? Enum.ToObject(ClrType.UnwrapNullableType(), value)
                : value;
    }
}

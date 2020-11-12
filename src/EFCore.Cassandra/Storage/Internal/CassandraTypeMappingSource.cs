// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Numerics;
using Cass = Cassandra;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraTypeMappingSource : RelationalTypeMappingSource
    {
        private const string IntegerTypeName = "int";
        private const string TextTypeName = "text";
        private const string LongTypeName = "bigint";
        private const string BlobTypeName = "blob";
        private const string BoolTypeName = "boolean";
        private const string DateTypeName = "date";
        private const string DecimalTypeName = "decimal";
        private const string DoubleTypeName = "double";
        private const string FloatTypeName = "float";
        private const string IpTypeName = "inet";
        private const string IntTypeName = "int";
        private const string ListTypeName = "list";
        private const string SmallIntTypeName = "smallint";
        private const string LocalTimeTypeName = "time";
        private const string TimestampTypeName = "timestamp";
        private const string TimeUuidTypeName = "timeuuid";
        private const string TinyIntTypeName = "tinyint";
        private const string BigIntegerTypeName = "varint";
        private const string GuidTypeName = "uuid";
        private const string DictionaryTypeName = "map";
        private static readonly CassandraTypeMapping<string> _text = new CassandraTypeMapping<string>(TextTypeName, string.Empty, DbType.String);
        private static readonly IntTypeMapping _integer = new IntTypeMapping(IntegerTypeName);
        private static readonly LongTypeMapping _long = new LongTypeMapping(LongTypeName);
        private static readonly CassandraTypeMapping<byte[]> _blob = new CassandraTypeMapping<byte[]>(BlobTypeName, new byte[0]);
        private static readonly BoolTypeMapping _bool = new BoolTypeMapping(BoolTypeName);
        private static readonly CassandraTypeMapping<LocalDate> _date = new CassandraTypeMapping<LocalDate>(DateTypeName, new LocalDate(1, 1, 1));
        private static readonly DecimalTypeMapping _decimal = new DecimalTypeMapping(DecimalTypeName);
        private static readonly DoubleTypeMapping _double = new DoubleTypeMapping(DoubleTypeName);
        private static readonly FloatTypeMapping _float = new FloatTypeMapping(FloatTypeName);
        private static readonly CassandraTypeMapping<IPAddress> _ipAddr = new CassandraTypeMapping<IPAddress>(IpTypeName, string.Empty, null, Cass.ColumnTypeCode.Inet);
        private static readonly ShortTypeMapping _short = new ShortTypeMapping(SmallIntTypeName);
        private static readonly CassandraTypeMapping<LocalTime> _localTime = new CassandraTypeMapping<LocalTime>(LocalTimeTypeName, new LocalTime(0, 0, 0, 0));
        private static readonly CassandraTypeMapping<DateTimeOffset> _timeStamp = new CassandraTypeMapping<DateTimeOffset>(TimestampTypeName, default(DateTimeOffset));
        private static readonly CassandraTypeMapping<TimeUuid> _timeUuid = new CassandraTypeMapping<TimeUuid>(TimeUuidTypeName, string.Empty, DbType.Guid);
        private static readonly SByteTypeMapping _sbyte = new SByteTypeMapping(TinyIntTypeName);
        private static readonly CassandraTypeMapping<BigInteger> _bigInteger = new CassandraTypeMapping<BigInteger>(BigIntegerTypeName, default(BigInteger));
        private static readonly GuidTypeMapping _guid = new GuidTypeMapping(GuidTypeName);

        private readonly Dictionary<Type, RelationalTypeMapping> _clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
        {
            { typeof(string), _text },
            { typeof(int), _integer },
            { typeof(long), _long},
            { typeof(byte[]), _blob},
            { typeof(bool), _bool },
            { typeof(LocalDate), _date },
            { typeof(decimal), _decimal },
            { typeof(double), _double },
            { typeof(float), _float},
            { typeof(IPAddress), _ipAddr },
            { typeof(short), _short },
            { typeof(LocalTime), _localTime },
            { typeof(DateTimeOffset), _timeStamp },
            { typeof(TimeUuid), _timeUuid },
            { typeof(sbyte), _sbyte },
            { typeof(BigInteger), _bigInteger },
            { typeof(Guid), _guid }
        };
        private readonly Dictionary<string, RelationalTypeMapping> _storeTypeMappings = new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
        {
            { TextTypeName, _text },
            { IntegerTypeName, _integer },
            { LongTypeName, _long },
            { BlobTypeName, _blob },
            { BoolTypeName, _bool },
            { DateTypeName, _date },
            { DecimalTypeName, _decimal },
            { DoubleTypeName, _double },
            { FloatTypeName, _float },
            { IpTypeName, _ipAddr },
            { LocalTimeTypeName, _localTime },
            { TimestampTypeName, _timeStamp },
            { TimeUuidTypeName, _timeUuid },
            { GuidTypeName, _guid },
            { BigIntegerTypeName, _bigInteger },
            { TinyIntTypeName, _sbyte }
        };

        public CassandraTypeMappingSource(TypeMappingSourceDependencies dependencies, RelationalTypeMappingSourceDependencies relationalDependencies) : base(dependencies, relationalDependencies)
        {
        }

        protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            if (clrType != null && _clrTypeMappings.TryGetValue(clrType, out var mapping))
            {
                return mapping;
            }

            if ((clrType.IsGenericType && (clrType.GetGenericTypeDefinition() == typeof(IEnumerable<>) || typeof(IEnumerable<>).IsAssignableFrom(clrType))) || clrType.IsArray)
            {
                Type genericType;
                if (clrType.IsGenericType)
                {
                    genericType = clrType.GenericTypeArguments.First();
                }
                else
                {
                    genericType = clrType.GetElementType();
                }

                var listTypeMappingType = typeof(CassandraListTypeMapping<>).MakeGenericType(genericType);
                var genericTypeName = _clrTypeMappings[genericType].StoreType;
                var result = (RelationalTypeMapping)Activator.CreateInstance(listTypeMappingType, $"{ListTypeName}<{genericTypeName}>", null);
                return result;
            }

            if ((clrType.IsGenericType && ((clrType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) || (clrType.GetGenericTypeDefinition() == typeof(IDictionary<,>)))))
            {
                Type firstGenericType = null, secondGenericType = null;
                if (clrType.IsGenericType)
                {
                    var genericTypes = clrType.GenericTypeArguments;
                    firstGenericType = genericTypes.First();
                    secondGenericType = genericTypes.Last();
                }
                else
                {
                }

                var listTypeMappingType = typeof(CassandraDicTypeMapping<,>).MakeGenericType(firstGenericType, secondGenericType);
                var firstGenericTypeName = _clrTypeMappings[firstGenericType].StoreType;
                var secondGenericTypeName = _clrTypeMappings[secondGenericType].StoreType;
                return (RelationalTypeMapping)Activator.CreateInstance(listTypeMappingType, $"{DictionaryTypeName}<{firstGenericTypeName},{secondGenericTypeName}>", null);
            }

                var storeTypeName = mappingInfo.StoreTypeName;
            if (storeTypeName != null && _storeTypeMappings.TryGetValue(storeTypeName, out mapping))
            {
                return mapping;
            }

            mapping = base.FindMapping(mappingInfo);
            return mapping != null
                ? mapping
                : storeTypeName != null
                    ? storeTypeName.Length != 0
                        ? _typeRules.Select(r => r(storeTypeName)).FirstOrDefault(r => r != null) ?? _text
                        : _text
                    : null;
        }

        private readonly Func<string, RelationalTypeMapping>[] _typeRules =
        {
            name => Contains(name, "int") ? _integer : null,
            name => Contains(name, "bigint") || Contains(name, "counter") ? _long : null,
            name => Contains(name, "blob") | Contains(name, "custom") ? _blob : null,
            name => Contains(name, "boolean") ? _bool : null,
            name => Contains(name, "timestamp") ? _timeStamp : null,
            name => Contains(name, "varint") ? _bigInteger : null,
            name => Contains(name, "timeuuid") ? _timeUuid : null,
            name => Contains(name, "time") ? _localTime : null,
            name => Contains(name, "decimal") ? _decimal : null,
            name => Contains(name, "inet") ? _ipAddr : null,
            name => Contains(name, "float") ? _float : null,
            name => Contains(name, "short") ? _short : null,
            name => Contains(name, "double") ? _double : null,
            name => Contains(name, "tinyint") ? _sbyte : null,
            name => Contains(name, "date") ? _date : null,
            name => Contains(name, "uuid") ? _guid : null,
            name => Contains(name, "ascii") || Contains(name, "text") || Contains(name, "varchar") ? _text : null
        };

        private static bool Contains(string haystack, string needle) => haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

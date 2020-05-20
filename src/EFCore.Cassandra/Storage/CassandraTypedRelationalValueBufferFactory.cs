// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage
{
    public class CassandraTypedRelationalValueBufferFactory : TypedRelationalValueBufferFactoryFactory
    {
        public CassandraTypedRelationalValueBufferFactory(RelationalValueBufferFactoryDependencies dependencies) : base(dependencies)
        {
        }

        private static readonly MethodInfo _isDbNullMethod
            = typeof(DbDataReader).GetTypeInfo().GetDeclaredMethod(nameof(DbDataReader.IsDBNull));

        private static readonly MethodInfo _getFieldValueMethod
            = typeof(DbDataReader).GetTypeInfo().GetDeclaredMethod(nameof(DbDataReader.GetFieldValue));

        private static readonly MethodInfo _throwReadValueExceptionMethod
            = typeof(TypedRelationalValueBufferFactoryFactory).GetTypeInfo()
                .GetDeclaredMethod(nameof(ThrowReadValueException));

        private readonly struct CacheKey
        {
            public CacheKey(IReadOnlyList<TypeMaterializationInfo> materializationInfo)
                => TypeMaterializationInfo = materializationInfo;

            public IReadOnlyList<TypeMaterializationInfo> TypeMaterializationInfo { get; }

            public override bool Equals(object obj)
                => obj is CacheKey cacheKey && Equals(cacheKey);

            private bool Equals(CacheKey other)
                => TypeMaterializationInfo.SequenceEqual(other.TypeMaterializationInfo);

            public override int GetHashCode()
                => TypeMaterializationInfo.Aggregate(0, (t, v) => (t * 397) ^ v.GetHashCode());
        }

        private readonly ConcurrentDictionary<CacheKey, TypedRelationalValueBufferFactory> _cache
            = new ConcurrentDictionary<CacheKey, TypedRelationalValueBufferFactory>();

        public override IRelationalValueBufferFactory Create(IReadOnlyList<TypeMaterializationInfo> types)
        {
            return _cache.GetOrAdd(
                new CacheKey(types),
                k => new TypedRelationalValueBufferFactory(
                    Dependencies,
                    CreateArrayInitializer(k, Dependencies.CoreOptions.AreDetailedErrorsEnabled)));
        }


        private static Func<DbDataReader, object[]> CreateArrayInitializer(CacheKey cacheKey, bool detailedErrorsEnabled)
            => Expression.Lambda<Func<DbDataReader, object[]>>(
                    Expression.NewArrayInit(
                        typeof(object),
                        cacheKey.TypeMaterializationInfo
                            .Select(
                                (mi, i) =>
                                    CreateGetValueExpression(
                                        DataReaderParameter,
                                        i,
                                        mi,
                                        detailedErrorsEnabled))),
                    DataReaderParameter)
                .Compile();

        private static Expression CreateGetValueExpression(
            Expression dataReaderExpression,
            int index,
            TypeMaterializationInfo materializationInfo,
            bool detailedErrorsEnabled,
            bool box = true)
        {
            var getMethod = materializationInfo.Mapping.GetDataReaderMethod();

            index = materializationInfo.Index == -1 ? index : materializationInfo.Index;

            var indexExpression = Expression.Constant(index);

            Expression valueExpression
                = Expression.Call(
                    getMethod.DeclaringType != typeof(DbDataReader)
                        ? Expression.Convert(dataReaderExpression, getMethod.DeclaringType)
                        : dataReaderExpression,
                    getMethod,
                    indexExpression);

            valueExpression = materializationInfo.Mapping.CustomizeDataReaderExpression(valueExpression);

            var converter = materializationInfo.Mapping.Converter;

            if (converter != null)
            {
                if (valueExpression.Type != converter.ProviderClrType)
                {
                    valueExpression = Expression.Convert(valueExpression, converter.ProviderClrType);
                }

                valueExpression = ReplacingExpressionVisitor.Replace(
                    converter.ConvertFromProviderExpression.Parameters.Single(),
                    valueExpression,
                    converter.ConvertFromProviderExpression.Body);
            }

            if (valueExpression.Type != materializationInfo.ModelClrType)
            {
                valueExpression = Expression.Convert(valueExpression, materializationInfo.ModelClrType);
            }

            var exceptionParameter
                = Expression.Parameter(typeof(Exception), name: "e");

            var property = materializationInfo.Property;

            if (detailedErrorsEnabled)
            {
                var catchBlock
                    = Expression
                        .Catch(
                            exceptionParameter,
                            Expression.Call(
                                _throwReadValueExceptionMethod
                                    .MakeGenericMethod(valueExpression.Type),
                                exceptionParameter,
                                Expression.Call(
                                    dataReaderExpression,
                                    _getFieldValueMethod.MakeGenericMethod(typeof(object)),
                                    indexExpression),
                                Expression.Constant(property, typeof(IPropertyBase))));

                valueExpression = Expression.TryCatch(valueExpression, catchBlock);
            }

            /*if (box && valueExpression.Type == typeof(TimeUuid))
            {
                valueExpression = Expression.Convert(valueExpression, typeof(TimeUuid));
            }
            else */if (box && valueExpression.Type.GetTypeInfo().IsValueType)
            {
                valueExpression = Expression.Convert(valueExpression, typeof(object));
            }

            if (property?.IsNullable != false
                || property.DeclaringEntityType.BaseType != null
                || materializationInfo.IsFromLeftOuterJoin != false)
            {
                valueExpression
                    = Expression.Condition(
                        Expression.Call(dataReaderExpression, _isDbNullMethod, indexExpression),
                        Expression.Default(valueExpression.Type),
                        valueExpression);
            }

            return valueExpression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TValue ThrowReadValueException<TValue>(
            Exception exception, object value, IPropertyBase property = null)
        {
            var expectedType = typeof(TValue);
            var actualType = value?.GetType();

            string message;

            if (property != null)
            {
                var entityType = property.DeclaringType.DisplayName();
                var propertyName = property.Name;

                message
                    = exception is NullReferenceException
                      || Equals(value, DBNull.Value)
                        ? CoreStrings.ErrorMaterializingPropertyNullReference(entityType, propertyName, expectedType)
                        : exception is InvalidCastException
                            ? CoreStrings.ErrorMaterializingPropertyInvalidCast(entityType, propertyName, expectedType, actualType)
                            : CoreStrings.ErrorMaterializingProperty(entityType, propertyName);
            }
            else
            {
                message
                    = exception is NullReferenceException
                        ? CoreStrings.ErrorMaterializingValueNullReference(expectedType)
                        : exception is InvalidCastException
                            ? CoreStrings.ErrorMaterializingValueInvalidCast(expectedType, actualType)
                            : CoreStrings.ErrorMaterializingValue;
            }

            throw new InvalidOperationException(message, exception);
        }
    }
}

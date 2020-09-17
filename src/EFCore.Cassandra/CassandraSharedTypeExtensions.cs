// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace System
{
    public static class CassandraSharedTypeExtensions
    {
        public static Expression UnwrapTypeConversion(this Expression expression, out Type convertedType)
        {
            convertedType = null;
            while (expression is UnaryExpression unaryExpression
                && unaryExpression.NodeType == ExpressionType.Convert)
            {
                expression = unaryExpression.Operand;
                if (unaryExpression.Type != typeof(object) // Ignore object conversion
                    && !unaryExpression.Type.IsAssignableFrom(expression.Type)) // Ignore casting to base type/interface
                {
                    convertedType = unaryExpression.Type;
                }
            }

            return expression;
        }

        public static bool IsValidEntityType(this Type type)
            => type.GetTypeInfo().IsClass;

        public static Type UnwrapNullableType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;

        public static bool IsInteger(this Type type)
        {
            type = type.UnwrapNullableType();
            return type == typeof(int)
                   || type == typeof(long)
                   || type == typeof(short)
                   || type == typeof(byte)
                   || type == typeof(uint)
                   || type == typeof(ulong)
                   || type == typeof(ushort)
                   || type == typeof(sbyte)
                   || type == typeof(char);
        }

        public static Type UnwrapEnumType(this Type type)
        {
            var isNullable = type.IsNullableType();
            var underlyingNonNullableType = isNullable ? type.UnwrapNullableType() : type;
            if (!underlyingNonNullableType.GetTypeInfo().IsEnum)
            {
                return type;
            }

            var underlyingEnumType = Enum.GetUnderlyingType(underlyingNonNullableType);
            return isNullable ? MakeNullable(underlyingEnumType) : underlyingEnumType;
        }

        public static Type MakeNullable(this Type type, bool nullable = true)
            => type.IsNullableType() == nullable
                ? type
                : nullable
                    ? typeof(Nullable<>).MakeGenericType(type)
                    : type.UnwrapNullableType();

        public static PropertyInfo GetAnyProperty(this Type type, string name)
        {
            var props = type.GetRuntimeProperties().Where(p => p.Name == name).ToList();
            if (props.Count > 1)
            {
                throw new AmbiguousMatchException();
            }

            return props.SingleOrDefault();
        }

        public static bool IsNullableType(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return !typeInfo.IsValueType
                   || typeInfo.IsGenericType
                   && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type TryGetSequenceType(this Type type)
            => type.TryGetElementType(typeof(IEnumerable<>))
               ?? type.TryGetElementType(typeof(IAsyncEnumerable<>));

        public static Type TryGetElementType(this Type type, Type interfaceOrBaseType)
        {
            if (type.GetTypeInfo().IsGenericTypeDefinition)
            {
                return null;
            }

            var types = GetGenericTypeImplementations(type, interfaceOrBaseType);

            Type singleImplementation = null;
            foreach (var impelementation in types)
            {
                if (singleImplementation == null)
                {
                    singleImplementation = impelementation;
                }
                else
                {
                    singleImplementation = null;
                    break;
                }
            }

            return singleImplementation?.GetTypeInfo().GenericTypeArguments.FirstOrDefault();
        }

        public static bool IsGrouping(this Type type) => IsGrouping(type.GetTypeInfo());
        
        public static Type GetSequenceType(this Type type)
        {
            var sequenceType = TryGetSequenceType(type);
            if (sequenceType == null)
            {
                // TODO: Add exception message
                throw new ArgumentException();
            }

            return sequenceType;
        }

        public static IEnumerable<Type> GetGenericTypeImplementations(this Type type, Type interfaceOrBaseType)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericTypeDefinition)
            {
                var baseTypes = interfaceOrBaseType.GetTypeInfo().IsInterface
                    ? typeInfo.ImplementedInterfaces
                    : type.GetBaseTypes();
                foreach (var baseType in baseTypes)
                {
                    if (baseType.GetTypeInfo().IsGenericType
                        && baseType.GetGenericTypeDefinition() == interfaceOrBaseType)
                    {
                        yield return baseType;
                    }
                }

                if (type.GetTypeInfo().IsGenericType
                    && type.GetGenericTypeDefinition() == interfaceOrBaseType)
                {
                    yield return type;
                }
            }
        }

        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            type = type.GetTypeInfo().BaseType;

            while (type != null)
            {
                yield return type;

                type = type.GetTypeInfo().BaseType;
            }
        }

        public static IEnumerable<PropertyInfo> GetPropertiesInHierarchy(this Type type, string name)
        {
            do
            {
                var typeInfo = type.GetTypeInfo();
                foreach (var propertyInfo in typeInfo.DeclaredProperties)
                {
                    if (propertyInfo.Name.Equals(name, StringComparison.Ordinal)
                        && !(propertyInfo.GetMethod ?? propertyInfo.SetMethod).IsStatic)
                    {
                        yield return propertyInfo;
                    }
                }

                type = typeInfo.BaseType;
            }
            while (type != null);
        }
    }
}

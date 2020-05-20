using System;

namespace System
{
    public static class SharedTypeExtensions
    {
        public static bool IsNullableType(this Type type)
            => !type.IsValueType || type.IsNullableValueType();
        public static bool IsNullableValueType(this Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}

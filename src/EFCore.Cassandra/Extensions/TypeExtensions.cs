using System;
using System.Collections.Generic;

namespace EFCore.Cassandra.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsList(this Type clrType)
        {
             return (clrType.IsGenericType &&
                (
                    typeof(IEnumerable<>).IsAssignableFrom(clrType) ||
                    clrType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    clrType.GetGenericTypeDefinition() == typeof(IList<>) ||
                    clrType.GetGenericTypeDefinition() == typeof(List<>)
            ) || clrType.IsArray);
        }
    }
}

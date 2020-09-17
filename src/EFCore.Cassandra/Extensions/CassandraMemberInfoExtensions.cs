using System.Linq;

namespace System.Reflection
{
    public static class CassandraMemberInfoExtensions
    {
        public static string GetSimpleMemberName(this MemberInfo member)
        {
            var name = member.Name;
            var index = name.LastIndexOf('.');
            return index >= 0 ? name.Substring(index + 1) : name;
        }
        public static bool IsSameAs(this MemberInfo propertyInfo, MemberInfo otherPropertyInfo)
            => propertyInfo == null
                ? otherPropertyInfo == null
                : (otherPropertyInfo == null
                    ? false
                    : Equals(propertyInfo, otherPropertyInfo)
                    || (propertyInfo.Name == otherPropertyInfo.Name
                        && (propertyInfo.DeclaringType == otherPropertyInfo.DeclaringType
                            || propertyInfo.DeclaringType.GetTypeInfo().IsSubclassOf(otherPropertyInfo.DeclaringType)
                            || otherPropertyInfo.DeclaringType.GetTypeInfo().IsSubclassOf(propertyInfo.DeclaringType)
                            || propertyInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces.Contains(otherPropertyInfo.DeclaringType)
                            || otherPropertyInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces
                                .Contains(propertyInfo.DeclaringType))));
    }
}

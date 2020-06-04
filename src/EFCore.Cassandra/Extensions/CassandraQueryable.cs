using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace System.Linq
{
    public static class CassandraQueryable
    {
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, bool allowFiltering = false)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (allowFiltering)
            {
                return source.Provider.CreateQuery<TSource>(
                   Expression.Call(
                       null,
                       Casandra_Where_TSource_2(typeof(TSource)),
                       source.Expression, Expression.Quote(predicate), Expression.Constant(allowFiltering)
                       ));

            }

            return source.Provider.CreateQuery<TSource>(
                   Expression.Call(
                       null,
                       Where_TSource_2(typeof(TSource)),
                       source.Expression, Expression.Quote(predicate)
                       ));
        }

        private static MethodInfo? s_Cassandra_Where_TSource_2;
        private static MethodInfo? s_Where_TSource_2;

        private static MethodInfo Casandra_Where_TSource_2(Type TSource) =>
             (s_Cassandra_Where_TSource_2 ??
             (s_Cassandra_Where_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, bool, IQueryable<object>>(CassandraQueryable.Where).GetMethodInfo().GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);

        private static MethodInfo Where_TSource_2(Type TSource) =>
             (s_Where_TSource_2 ??
             (s_Where_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where).GetMethodInfo().GetGenericMethodDefinition()))
              .MakeGenericMethod(TSource);
    }
}

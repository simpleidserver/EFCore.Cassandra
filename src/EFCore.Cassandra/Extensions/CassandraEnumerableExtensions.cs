// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Utilities
{
    public static class CassandraEnumerableExtensions
    {
        public static int IndexOf<T>([NotNull] this IEnumerable<T> source, [NotNull] T item)
            => IndexOf(source, item, EqualityComparer<T>.Default);

        public static int IndexOf<T>(
            [NotNull] this IEnumerable<T> source,
            [NotNull] T item,
            [NotNull] IEqualityComparer<T> comparer)
            => source.Select(
                    (x, index) =>
                        comparer.Equals(item, x) ? index : -1)
                .FirstOr(x => x != -1, -1);

        public static T FirstOr<T>([NotNull] this IEnumerable<T> source, [NotNull] T alternate)
            => source.DefaultIfEmpty(alternate).First();

        public static T FirstOr<T>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, bool> predicate, [NotNull] T alternate)
            => source.Where(predicate).FirstOr(alternate);

        public static string Join(
            [NotNull] this IEnumerable<object> source,
            [NotNull] string separator = ", ")
            => string.Join(separator, source);
    }
}

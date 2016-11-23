namespace Evaders.Server.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class LinqExtensions
    {
        public static T MinOrDefault<T>(this IEnumerable<T> enumerable, Func<T, IComparable> selector)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                var min = default(T);
                if (!enumerator.MoveNext())
                    return min;
                var minKey = selector(enumerator.Current);
                min = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    var cmpValue = selector(enumerator.Current);
                    if (cmpValue.CompareTo(minKey) < 1)
                        continue;
                    minKey = cmpValue;
                    min = enumerator.Current;
                }

                return min;
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> selector)
        {
            var existing = new HashSet<TKey>();
            return enumerable.Where(source => existing.Add(selector(source)));
        }
    }
}
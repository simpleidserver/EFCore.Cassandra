namespace System.Collections.Generic
{
    public static class CassandraDictionaryExtensions
    {
        public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
            where TValue : new()
        {
            if (!source.TryGetValue(key, out var value))
            {
                value = new TValue();
                source.Add(key, value);
            }

            return value;
        }

        public static TValue Find<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key)
            => !source.TryGetValue(key, out var value) ? default : value;
    }
}

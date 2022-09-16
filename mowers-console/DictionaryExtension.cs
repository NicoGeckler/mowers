using System.Diagnostics.CodeAnalysis;

namespace CustomMower;

internal static class DictionaryExtension
{
    public static bool TryGetAs<TKey, TItem, TTarget>(
        this Dictionary<TKey, TItem> dictionary,
        TKey key,
        [NotNullWhen(true)] out TTarget? value
    )
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out TItem? rawValue) && rawValue is TTarget castValue)
        {
            value = castValue;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
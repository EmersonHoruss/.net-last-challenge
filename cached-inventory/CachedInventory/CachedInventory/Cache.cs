namespace CachedInventory;

using System.Collections.Concurrent;

public interface ICache
{
    bool Exists(int key);
    int GetValue(int key);
    void AddOrUpdateValue(int key, int value);
}


public class Cache : ICache
{
    private readonly ConcurrentDictionary<int, int> dictionary = new();

    public bool Exists(int key) => dictionary.TryGetValue(key, out var _);

    public int GetValue(int key)
    {
        _ = dictionary.TryGetValue(key, out var value);
        return value;
    }

    public void AddOrUpdateValue(int key, int value) => dictionary.AddOrUpdate(key, value, (k, oldValue) => value);
}

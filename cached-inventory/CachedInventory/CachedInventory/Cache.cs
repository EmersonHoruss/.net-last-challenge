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
    private readonly IWarehouseStockSystemClient client;
    public Cache(IWarehouseStockSystemClient client) => this.client = client;

    public bool Exists(int key) => dictionary.TryGetValue(key, out var _);

    public int GetValue(int key)
    {
        _ = dictionary.TryGetValue(key, out var value);
        return value;
    }

    public void AddOrUpdateValue(int key, int delta) =>
        dictionary.AddOrUpdate(key, delta, (k, oldValue) =>
        {
            var newValue = oldValue + delta;
            _ = Task.Run(() => _ = client.UpdateStock(k, newValue));
            return newValue;
        });
}

using Orleans;
using Orleans.Runtime;

class FeedItemResultGrain : Grain, IFeedItemResults
{
    private readonly IPersistentState<FeedItemStore> _storage;

    public FeedItemResultGrain([PersistentState("feed-item-results", "redis-rss-reader")] IPersistentState<FeedItemStore> storage) => _storage = storage;

    public async Task AddAsync(List<FeedItem> items)
    {
        //make sure there is no duplication
        foreach (var i in items.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
        {
            if (!_storage.State.Results.Exists(x => x.Id?.Equals(i.Id, StringComparison.OrdinalIgnoreCase) ?? false))
                _storage.State.Results.Add(i);
        }
        await _storage.WriteStateAsync();
    }

    public Task<List<FeedItem>> GetAllAsync() => Task.FromResult(_storage.State.Results.OrderByDescending(x => x.PublishedOn).ToList());

    public async Task ClearAsync()
    {
        _storage.State.Results.Clear();
        await _storage.WriteStateAsync();
    }
}

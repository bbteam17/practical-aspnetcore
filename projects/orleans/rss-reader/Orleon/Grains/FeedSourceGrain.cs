using Orleans;
using Orleans.Runtime;

class FeedSourceGrain : Grain, IFeedSource
{
    private readonly IPersistentState<FeedSourceStore> _storage;

    public FeedSourceGrain([PersistentState("feed-source", "redis-rss-reader")] IPersistentState<FeedSourceStore> storage) => _storage = storage;

    public async Task AddAsync(FeedSource source)
    {
        if (_storage.State.Sources.Find(x => x.Url == source.Url) is null)
        {
            _storage.State.Sources.Add(source);
            await _storage.WriteStateAsync();
        }
    }

    public Task<List<FeedSource>> GetAllAsync() => Task.FromResult(_storage.State.Sources);
}
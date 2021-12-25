interface IFeedItemResults : Orleans.IGrainWithIntegerKey
{
    Task AddAsync(List<FeedItem> items);

    Task<List<FeedItem>> GetAllAsync();

    Task ClearAsync();
}

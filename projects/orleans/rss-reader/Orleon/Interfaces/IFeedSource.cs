interface IFeedSource : Orleans.IGrainWithIntegerKey
{
    Task AddAsync(FeedSource source);

    Task<List<FeedSource>> GetAllAsync();
}
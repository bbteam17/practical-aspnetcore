interface IFeedFetcher : Orleans.IGrainWithStringKey
{
    Task FetchAsync(FeedSource source);
}
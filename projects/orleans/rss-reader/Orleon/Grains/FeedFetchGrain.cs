
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;
using Orleans;
using System.Xml;

class FeedFetchGrain : Grain, IFeedFetcher
{
    readonly IGrainFactory _grainFactory;

    public FeedFetchGrain(IGrainFactory grainFactory) => _grainFactory = grainFactory;

    public async Task FetchAsync(FeedSource source)
    {
        var storage = _grainFactory.GetGrain<IFeedItemResults>(0);
        var results = await ReadFeedAsync(source);
        await storage.AddAsync(results);
    }

    public async Task<List<FeedItem>> ReadFeedAsync(FeedSource source)
    {
        var feedList = new List<FeedItem>();

        try
        {
            using var xmlReader = XmlReader.Create(source.Url.ToString(), new XmlReaderSettings() { Async = true });
            if (source.Type == FeedType.Rss)
            {
                var feedReader = new RssFeedReader(xmlReader);

                // Read the feed
                while (await feedReader.Read())
                {
                    switch (feedReader.ElementType)
                    {
                        // Read Item
                        case SyndicationElementType.Item:
                            var item = await feedReader.ReadItem();
                            feedList.Add(new FeedItem(source.ToChannel(), new SyndicationItem(item)));
                            break;

                        default:
                            var content = await feedReader.ReadContent();

                            break;
                    }
                }
            }
            else
            {
                var feedReader = new AtomFeedReader(xmlReader);

                while (await feedReader.Read())
                {
                    switch (feedReader.ElementType)
                    {
                        // Read Item
                        case SyndicationElementType.Item:
                            var entry = await feedReader.ReadEntry();
                            feedList.Add(new FeedItem(source.ToChannel(), new SyndicationItem(entry)));
                            break;

                        default:
                            var content = await feedReader.ReadContent();
                            break;
                    }
                }
            }

            return feedList;
        }
        catch
        {
            return new List<FeedItem>();
        }
    }
}

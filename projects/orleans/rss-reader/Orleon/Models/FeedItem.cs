

using Microsoft.SyndicationFeed;

record FeedItem
{
    public FeedChannel? Channel { get; set; }

    public string? Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public Uri? Url { get; set; }

    public DateTimeOffset PublishedOn { get; set; }

    public FeedItem()
    {

    }

    public FeedItem(FeedChannel channel, SyndicationItem item)
    {
        Channel = channel;
        Id = item.Id;
        Title = item.Title;
        Description = item.Description;
        var link = item.Links.FirstOrDefault();
        if (link is object)
            Url = link.Uri;

        if (item.LastUpdated == default(DateTimeOffset))
            PublishedOn = item.Published;
        else
            PublishedOn = item.LastUpdated;
    }
}

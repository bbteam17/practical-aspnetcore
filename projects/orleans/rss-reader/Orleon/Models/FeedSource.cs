class FeedSource
{
    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Website { get; set; }

    public FeedType Type { get; set; }

    public bool HideTitle { get; set; }

    public bool HideDescription { get; set; }

    public FeedChannel ToChannel()
    {
        return new FeedChannel
        {
            Title = Title,
            Website = Website,
            HideTitle = HideTitle,
            HideDescription = HideDescription
        };
    }
}

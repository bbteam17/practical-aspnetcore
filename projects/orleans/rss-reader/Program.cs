using System.Net;
using Orleans;
using Orleans.Runtime;
using Orleans.Configuration;
using Orleans.Hosting;
using System.Xml;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;

var builder = WebApplication.CreateBuilder();
builder.Services.AddHttpClient();
builder.Logging.SetMinimumLevel(LogLevel.Debug).AddConsole();
builder.Host.UseOrleans(builder =>
{
    builder
        .UseLocalhostClustering()
        .UseInMemoryReminderService()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "http-client";
        })
        .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(FeedSourceGrain).Assembly).WithReferences())
        .AddRedisGrainStorage("redis-rss-reader", optionsBuilder => optionsBuilder.Configure(options =>
        {
            options.ConnectionString = "localhost:6379";
            options.UseJson = true;
            options.DatabaseNumber = 1;
        }));
});

var app = builder.Build();

app.MapGet("/",Get);

app.Run();

async Task Get(HttpContext context)
{
    
        var client = context.RequestServices.GetService<IGrainFactory>()!;
        var feedSourceGrain = client.GetGrain<IFeedSource>(0)!;
        //await feedSourceGrain.AddAsync(new FeedSource
        //{
        //    Type = FeedType.Rss,
        //    Url = "http://www.scripting.com/rss.xml",
        //    Website = "http://www.scripting.com",
        //    Title = "Scripting News"
        //});
    await feedSourceGrain.AddAsync(new FeedSource
    {
        Type = FeedType.Rss,
        Url = "https://techcommunity.microsoft.com/gxcuf89792/rss/message?board.id=TeamsDeveloper&message.id=4505",
        Website = "https://techcommunity.microsoft.com",
        Title = "Teams News"
    });

    //await feedSourceGrain.AddAsync(new FeedSource
    //    {
    //        Type = FeedType.Atom,
    //        Url = "https://www.reddit.com/r/dotnet.rss",
    //        Website = "https://www.reddit.com/r/dotnet",
    //        Title = "Reddit/r/dotnet"
    //    });

        var sources = await feedSourceGrain.GetAllAsync();

        foreach (var s in sources)
        {
            var feedFetcherGrain = client.GetGrain<IFeedFetcher>(s.Url.ToString());
            await feedFetcherGrain.FetchAsync(s);
        }

        var feedResultsGrain = client.GetGrain<IFeedItemResults>(0);
        var feedItems = await feedResultsGrain.GetAllAsync();

        await context.Response.WriteAsync(@"<html>
                    <head>
                        <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/uikit@3.5.5/dist/css/uikit.min.css"" />
                        <title>Orleans RSS Reader</title>
                    </head>");
        await context.Response.WriteAsync("<body><div class=\"uk-container\">");
        await context.Response.WriteAsync("<ul class=\"uk-list\">");
        foreach (var i in feedItems)
        {
            await context.Response.WriteAsync("<li class=\"uk-card uk-card-default uk-card-body\">");
            if (!string.IsNullOrWhiteSpace(i.Title))
                await context.Response.WriteAsync($"{ i.Title }<br/>");

            await context.Response.WriteAsync(i.Description ?? "");

            if (i.Url is object)
                await context.Response.WriteAsync($"<br/><a href=\"{i.Url}\">link</a>");

            await context.Response.WriteAsync($"<div style=\"font-size:small;\">published on: {i.PublishedOn}</div>");
            await context.Response.WriteAsync($"<div style=\"font-size:small;\">source: <a href=\"{i.Channel?.Website}\">{i.Channel?.Title}</a></div>");
            await context.Response.WriteAsync("</li>");
        }
        await context.Response.WriteAsync("</ul>");
        await context.Response.WriteAsync("</div></body></html>");
    
}
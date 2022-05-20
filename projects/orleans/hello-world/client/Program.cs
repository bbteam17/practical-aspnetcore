using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

await new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ConfigService>();
        services.AddSingleton<IHostedService>(provider => provider.GetService<ConfigService>()!);
        services.AddSingleton(provider => provider.GetService<ConfigService>()!.Client);

        services.AddHostedService<SenderService>();

        services.Configure<ConsoleLifetimeOptions>(options =>
        {
            options.SuppressStatusMessages = true;
        });
    })
    .ConfigureLogging(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddConsole();
    })
    .RunConsoleAsync();

public class SenderService : IHostedService
{
    private readonly IClusterClient _client;

    public SenderService(IClusterClient client)
    {
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"*** *** ==> {nameof(SenderService)}.{nameof(StartAsync)} ...");
        // example of calling grains from the initialized client
     
        var friend1 = _client.GetGrain<IHello>(0);
        var response = await friend1.SayHello($"{friend1.GetPrimaryKeyLong()} => Hallo!");
        Console.WriteLine($"{response}");
        response = await friend1.SayHello($"{friend1.GetPrimaryKeyLong()} => Hallo2");
        Console.WriteLine($"{response}");

        var friend2 = _client.GetGrain<IHello>(1);
        response = await friend2.SayHello($"{friend2.GetPrimaryKeyLong()} => Hallo!");
        Console.WriteLine($"{response}");
        response = await friend2.SayHello($"{friend2.GetPrimaryKeyLong()} => Hallo2");
        Console.WriteLine($"{response}");

        // example of calling IHelloArchive grqain that implements persistence
        var g = this._client.GetGrain<IHelloArchive>($"{friend1.GetPrimaryKeyLong()}");
        var greetings = await g.GetGreetings() ;
        Log(greetings.ToList());

        g = this._client.GetGrain<IHelloArchive>($"{friend2.GetPrimaryKeyLong()}");
          greetings = await g.GetGreetings();
        Log(greetings.ToList());
        // Console.WriteLine($"\nArchived greetings: {Utils.EnumerableToString(greetings,separator:"\n")}");
    }

    private void Log(List<string> items)
    {
        Console.WriteLine("\n\n");
        foreach (var item in items)
        {
            Console.WriteLine($" \n *** {item}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class ConfigService : IHostedService
{
    private readonly ILogger<ConfigService> _logger;

    public ConfigService(ILogger<ConfigService> logger, ILoggerProvider loggerProvider)
    {
        _logger = logger;
        Client = new ClientBuilder()
            .UseLocalhostClustering()
            .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"*** *** ==>  {nameof(ConfigService)}.{nameof(ConfigService)} ...");
        var attempt = 0;
        var maxAttempts = 100;
        var delay = TimeSpan.FromSeconds(1);

        async Task<bool> retryFilter(Exception error)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromResult(false);
            }

            if (++attempt < maxAttempts)
            {
                _logger.LogWarning(error,
                    "Failed to connect to Orleans cluster on attempt {@Attempt} of {@MaxAttempts}.",
                    attempt, maxAttempts);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }

                return true;
            }
            else
            {
                _logger.LogError(error,
                    "Failed to connect to Orleans cluster on attempt {@Attempt} of {@MaxAttempts}.",
                    attempt, maxAttempts);

                return false;
            }
        }
        await Client.Connect(retryFilter);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Client.Close();
        }
        catch (OrleansException error)
        {
            _logger.LogWarning(error, "Error while gracefully disconnecting from Orleans cluster. Will ignore and continue to shutdown.");
        }
    }

    public IClusterClient Client { get; }
}
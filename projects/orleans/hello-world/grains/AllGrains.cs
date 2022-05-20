using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System.Collections.Generic;

public class HelloGrain : Grain, IHello
{
    private readonly ILogger _logger;

    private readonly IClusterClient client;
    public HelloGrain(ILogger<HelloGrain> logger, IClusterClient client)
    {
        _logger = logger;
        this.client = client;
    }  

    async Task<string> IHello.SayHello(string greeting )
    {
        _logger.LogInformation($" SayHello message received: greeting = '{greeting}'");
         var history = client.GetGrain<IHelloArchive>($"{this.GetPrimaryKeyLong()}");
         await  history.AddArchive($"{greeting}");
        return await Task.FromResult($"HelloGrain => You said: '{greeting}', I say: Hello!");
    }
}

public class HelloArchiveGrain : Grain, IHelloArchive
{
    private readonly IPersistentState<GreetingArchive> _archive;

    public HelloArchiveGrain([PersistentState("archive", "ArchiveStorage")] IPersistentState<GreetingArchive> archive)
    {
        _archive = archive;
    }

    public async Task AddArchive(string archive)
    {
        _archive.State.Greetings.Add(archive);
        await _archive.WriteStateAsync();
    }

    public async Task<IEnumerable<string>> GetGreetings() => await Task.FromResult<IEnumerable<string>>(_archive.State.Greetings);
}

public class GreetingArchive
{
    public List<string> Greetings { get; } = new List<string>();
}
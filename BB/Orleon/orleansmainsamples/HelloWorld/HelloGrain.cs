using System.Threading.Tasks;
using Orleans;

namespace HelloWorld
{
    public class HelloGrain : Grain, IHelloGrain
    {
        public async  Task<string> SayGoodBye(string msg) => await Task.FromResult(msg);

        public Task<string> SayHello(string greeting) => Task.FromResult($"Hello, {greeting}!");
    }
}

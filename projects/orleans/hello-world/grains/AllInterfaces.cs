using System.Collections.Generic;
using System.Threading.Tasks;

public interface IHello : Orleans.IGrainWithIntegerKey
{
    Task<string> SayHello(string greeting);
}

public interface IHelloArchive : Orleans.IGrainWithStringKey
{
    Task  AddArchive(string archive);

    Task<IEnumerable<string>> GetGreetings();
}
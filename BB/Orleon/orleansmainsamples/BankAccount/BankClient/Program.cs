using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using AccountTransfer.Interfaces;
using System.Collections.Generic;
using System.Linq;

using var client = new ClientBuilder()
    .UseLocalhostClustering()
    .ConfigureLogging(logging => logging.AddConsole())
    .Build();

await client.Connect();

var fromNames = new[] { "F-A", "F-B", "F-C", "F-D" };
var toNames = new[] { "t1", "t2", "t3", "t4" };
var fromdictionary = new Dictionary<string, uint>();
var todictionary = new Dictionary<string, uint>();
for (int i = 0; i < fromNames.Length; i++)
{
    var from = client.GetGrain<IAccountGrain>(fromNames[i]);
    var balance = await from.GetBalance();
    fromdictionary.Add(fromNames[i], balance);
}

for (int i = 0; i < toNames.Length; i++)
{
    var to = client.GetGrain<IAccountGrain>(toNames[i]);
    var balance = await to.GetBalance();
    todictionary.Add(toNames[i], balance);
}

Func<bool> isAvaliab = () => fromdictionary.Values.Any(x => x > 0);

var random = new Random();
while (!Console.KeyAvailable && isAvaliab())
{
    var atm = client.GetGrain<IAtmGrain>(0);

    // Choose some random accounts to exchange money
    var fromId = random.Next(fromNames.Length);
    var toId = random.Next(toNames.Length);

    while (fromdictionary[fromNames[fromId]] == 0 && isAvaliab())
    {

        fromId = (fromId + 1) % fromNames.Length;
    }

    var fromName = fromNames[fromId];
    var toName = toNames[toId];
    var from = client.GetGrain<IAccountGrain>(fromName);
    var to = client.GetGrain<IAccountGrain>(toName);

    // Perform the transfer and query the results
    try
    {
        var availabe = await from.AvaliableDraw(100);
        if (availabe)
        {
            await atm.Transfer(from, to, 100);

        }
        var fromBalance = await from.GetBalance();
        var toBalance = await to.GetBalance();
        fromdictionary[fromName] = fromBalance;
        todictionary[toName] = toBalance;

        if (availabe)
        {
            Console.WriteLine($"We transfered 100 credits from {fromName} to {toName}.\n{fromName} balance: {fromBalance}\n{toName} balance: {toBalance}\n");
        }
        else
        {
            Console.WriteLine($"No transfer {fromName} to {toName}.\n{fromName} balance: {fromBalance}\n{toName} balance: {toBalance}\n");
        }

    }
    catch (Exception exception)
    {
        Console.WriteLine($"Error transfering 100 credits from {fromName} to {toName}: {exception.Message}");
        if (exception.InnerException is { } inner)
        {
            Console.WriteLine($"\tInnerException: {inner.Message}\n");
        }

        Console.WriteLine();
    }

    // Sleep and run again
    await Task.Delay(10);
}

Console.WriteLine("*** Transaction Finish");
Console.WriteLine($"*** *** *** Latest");

var values = todictionary.Values.ToList();
values = values.OrderByDescending(x => x).ToList();
for (int i = 0; i < values.Count; i++)
{
    var toITem =todictionary.FirstOrDefault(x=> x.Value == values[i]);
    Console.WriteLine($"\t {toITem.Key} => {toITem.Value}");
}
Console.Read();
namespace Samples.Client;

using System;
using System.Net;
using System.Threading.Tasks;
using SlimMessenger;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("SlimMessenger - Client");

        var client = new SlimClient();

        client.ClientDisconnected += client => Console.WriteLine("Client disconnected");
        client.ClientConnectedToEndPoint += (client, success, serverIP, serverPort)
            => Console.WriteLine($"Client connected to end point: {success} - {serverIP} port: {serverPort}");
        client.ClientConnected += client => Console.WriteLine("Client connected");

        try
        {
            client.Connect("127.0.0.1").Wait();
            ClientRunLoop(client).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static async Task ClientRunLoop(SlimClient client)
    {
        var serverMessage = await client.ReadAsync();
        Console.WriteLine(serverMessage);
        while (client.IsConnected)
        {
            var sendMsg = Console.ReadLine()!;
            sendMsg = sendMsg.Replace('`', '\0');
            if (sendMsg == "") client.Disconnect();
            client.WriteAsync(sendMsg).Wait();
        }
    }
}


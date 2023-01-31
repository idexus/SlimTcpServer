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

        client.ClientConnected += client => Console.WriteLine("Client connected");
        client.ClientDisconnected += client => Console.WriteLine("Client disconnected");
        client.ConnectedToServerEndPoint += (bool success, IPAddress serverIP, int serverPort)
            => Console.WriteLine($"Connected to end point: {success} - {serverIP} port: {serverPort}");

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


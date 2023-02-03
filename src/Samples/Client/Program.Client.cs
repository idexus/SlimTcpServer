namespace Samples.Client;

using System;
using System.Net;
using System.Threading.Tasks;
using SlimTcpServer;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("SlimTcpServer - Client");

        var client = new SlimClient();

        client.Disconnected += client => Console.WriteLine("Client disconnected");
        client.Connected += client => Console.WriteLine("Client connected");

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
            if (sendMsg == "")
            {
                await client.Disconnect();
                return;
            }            
            await client.WriteAsync(sendMsg);
            if (sendMsg == "play")
            {
                var message = await client.ReadAsync();
                Console.WriteLine(message);
            }
        }
    }
}


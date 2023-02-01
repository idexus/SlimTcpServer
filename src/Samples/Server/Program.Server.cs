namespace Samples.Server;

using System;
using System.Threading.Tasks;
using SlimMessenger;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("SlimMessenger - Server");

            var server = new SlimServer();

            server.ServerStarted += server => Console.WriteLine($"Server started");
            server.ServerStopped += server => Console.WriteLine($"Server stopped");
            server.ClientConnected += client => Server_ClientConnected(client);
            server.ClientDisconnected += client => Console.WriteLine($"Client disconnected: {client.Guid}");           

            server.Start();

            Console.ReadLine();
            server.Stop();
            while (server.IsRunning) Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void Server_ClientConnected(SlimClient client)
    {
        Console.WriteLine($"Client connected: {client.Guid}");
        client.RunLoop += Client_RunLoop;
    }

    static async Task Client_RunLoop(SlimClient client)
    {
        await client.WriteAsync($"Hello, your Guid: {client.Guid}");
        while (client.IsConnected)
        {
            var message = await client.ReadAsync();
            Console.WriteLine($@"Client: {client.Guid}, data received : ""{message}""");
            if (message == "close")
            {
                Console.WriteLine($"Client close request");
                client.Disconnect();
            }
            else if (message == "play")
            {
                await client.WriteAsync("Lets play");
            }
        }
    }
}

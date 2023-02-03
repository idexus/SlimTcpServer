namespace Samples.Server;

using System;
using System.Threading.Tasks;
using SlimTcpServer;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("SlimTcpServer - Server");

            var server = new SlimServer();

            server.ServerStarted += server => Console.WriteLine($"Server started on port: {server.ServerPort}");
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
        _ = ClientRunLoop(client);
    }

    static async Task ClientRunLoop(SlimClient client)
    {
        await client.WriteAsync($"Hello, your Guid: {client.Guid}");
        while (client.IsConnected)
        {
            var message = await client.ReadAsync();
            Console.WriteLine($@"Client: {client.Guid}, data received : ""{message}""");
            if (message == "close")
            {
                Console.WriteLine($"Client close request");
                await client.Disconnect();
            }
            else if (message == "play")
            {
                await client.WriteAsync("Lets play");
            }
        }
    }
}

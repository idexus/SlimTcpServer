namespace Samples.Server
{
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

                var sever = new SlimServer();

                sever.ServerStarted += server => Console.WriteLine($"Server started on port: {server.ServerPort}");
                sever.ServerStopped += server => Console.WriteLine($"Server stopped");
                sever.ClientConnected += client => Server_ClientConnected(client);
                sever.ClientDisconnected += client => Console.WriteLine($"Client disconnected: {client.Guid}");

                sever.StartAsync().Wait();

                Console.ReadLine();
                sever.StopAsync().Wait();
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
                    await client.DisconnectAsync();
                }
                else if (message == "play")
                {
                    await client.WriteAsync("Lets play");
                }
            }
        }
    }
}
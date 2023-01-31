namespace Samples.Server;

using System;
using System.Threading.Tasks;
using TcpServerSlim;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("TcpServer.Slim - Server");

            var server = new TcpServerSlim();

            server.ServerStarted += server => Console.WriteLine($"Server started");
            server.ServerStopped += server => Console.WriteLine($"Server stopped");
            server.ClientConnected += Server_ClientConnected;
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

    static void Server_ClientConnected(TcpClientSlim client)
    {
        client.DataReceived += Client_DataReceived;
        Console.WriteLine($"Client connected: {client.Guid}");
    }

    static void Client_DataReceived(TcpClientSlim client, string message)
    {
        Console.WriteLine($@"Client: {client.Guid}, data received : ""{message}""");
        if (message == "close")
        {
            Console.WriteLine($"Client close request");
            client.Disconnect();
        }
        else if (message == "play")
        {
            Task.Run(() => client.WriteAsync("Lets play"));
        }
    }
}

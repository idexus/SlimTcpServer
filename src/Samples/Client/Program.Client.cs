namespace Samples.Client;

using System;
using System.Net;
using SlimMessenger;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("SlimMessenger - Client");

        var client = new SlimClient();

        client.DataReceived += Client_DataReceived;
        client.ClientConnected += client => Console.WriteLine("Client connected");
        client.ClientDisconnected += client => Console.WriteLine("Client disconnected");

        try
        {
            client.Connect("127.0.0.1").Wait();
            SendMessages(client);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void Client_DataReceived(SlimClient client, string message)
    {
        Console.WriteLine($@"Server sent data : ""{message}""");
    }

    static void SendMessages(SlimClient client)
    {
        while (!client.IsDisconnectionRequested)
        {
            var sendMsg = Console.ReadLine()!;
            sendMsg = sendMsg.Replace('`', '\0');
            if (sendMsg == "") client.Disconnect();
            client.WriteAsync(sendMsg).Wait();
        }
    }
}


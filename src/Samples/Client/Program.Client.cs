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

        client.Disconnected += client => Console.WriteLine("Client disconnected");
        client.ConnectedToEndPoint += Client_ConnectedToEndPoint;
        client.Connected += client => Console.WriteLine("Client connected");
        client.RunLoop += Client_RunLoop;

        try
        {
            client.Connect("127.0.0.1").Wait();
            client.WaitForDisconnection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task<bool> Client_ConnectedToEndPoint(SlimClient client, bool success, IPAddress serverIP, int serverPort)
    {
        Console.WriteLine($"address: {serverIP} port: {serverPort} success: {(success ? "YES" : "NO")}");
        await Task.Delay(100); // do some connection stuff
        return success;
    }

    static async Task Client_RunLoop(SlimClient client)
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


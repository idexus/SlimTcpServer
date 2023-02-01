# SlimMessenger

A simple TCP string messenger

# Example Usage

## Client

```cs
using SlimMessenger;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("SlimMessenger - Client");

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
            if (sendMsg == "") client.Disconnect();
            client.WriteAsync(sendMsg).Wait();
        }
    }
}
```

## Server

```cs
using SlimMessenger;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("SlimMessenger - Server");

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
                client.Disconnect();
            }
            else if (message == "play")
            {
                await client.WriteAsync("Lets play");
            }
        }
    }
}
```

## Console output

Client side

```
SlimMessenger - Client
Client connected
Hello, your Guid: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c
Hello, I'm your client
This is`a test
play
close
Client disconnected
```

Server side

```
SlimMessenger - Server
Server started
Client connected: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "Hello, I'm your client"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "This is"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "a test"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "play"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "close"
Client close request
The operation was canceled.
Client disconnected: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c
```

# License 

[MIT License](LICENSE), Copyright (c) 2023 Pawel Krzywdzinski

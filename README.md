# SlimTcpServer

A simple TCP server and client library

# Example Usage

## Server

```cs
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
```

## Client

```cs
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
            client.ConnectAsync("127.0.0.1").Wait();
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
            var sendMsg = Console.ReadLine();
            sendMsg = sendMsg.Replace('`', '\0');
            if (sendMsg == "")
            {
                await client.DisconnectAsync();
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
```

## Console output

Client side

```
SlimTcpServer - Client
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
SlimTcpServer - Server
Server started on port: 5085
Client connected: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "Hello, I'm your client"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "This is"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "a test"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "play"
Client: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c, data received : "close"
Client close request
Client disconnected: 1eb630fa-148d-41d4-a7c9-7c9f0a1d014c
```

# Nuget

.Net CLI

```
dotnet add package SlimTcpServer --version 0.2.2-beta.4
```

# Disclaimer

There is no official support. Use at your own risk.

# License 

[MIT License](LICENSE), Copyright (c) 2023 Pawel Krzywdzinski

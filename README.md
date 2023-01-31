# SlimMessenger

A simple TCP string messaganger

# Example Usage

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

    static void Server_ClientConnected(SlimClient client)
    {
        client.DataReceived += Client_DataReceived;
        Console.WriteLine($"Client connected: {client.Guid}");
    }

    static void Client_DataReceived(SlimClient client, string message)
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
```

## Client

```cs
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
```

## Console output

Client side

```
SlimMessenger - Client
Client connected
Hello, World!
This is`a test
play
Server sent data : "Let's play"
close
Client disconnected
```

Server side

```
SlimMessenger - Server
Server started
Client connected: 342509ba-4c5b-4f90-93e6-8ea3bca363d5
Client: 342509ba-4c5b-4f90-93e6-8ea3bca363d5, data received : "Hello, World!"
Client: 342509ba-4c5b-4f90-93e6-8ea3bca363d5, data received : "This is"
Client: 342509ba-4c5b-4f90-93e6-8ea3bca363d5, data received : "a test"
Client: 342509ba-4c5b-4f90-93e6-8ea3bca363d5, data received : "play"
Client: 342509ba-4c5b-4f90-93e6-8ea3bca363d5, data received : "close"
Client close request
Client disconnected: 342509ba-4c5b-4f90-93e6-8ea3bca363d5
```

# License 

[MIT License](LICENSE), Copyright (c) 2023 Pawel Krzywdzinski

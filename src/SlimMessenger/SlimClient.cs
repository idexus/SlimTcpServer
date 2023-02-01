using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SlimMessenger;

public class NoConnectionException : Exception { }

public class SlimClient
{
    public const int DefaultTimeout = 1000;

    // private

    readonly ConcurrentQueue<string> messagesQueue = new ConcurrentQueue<string>();
    TcpClient logicClient;
    CancellationTokenSource cancellationTokenSource;
    SemaphoreSlim messagesSemaphore = new SemaphoreSlim(0);

    // public

    public Guid? Guid { get; private set; }
    public IPAddress? ServerIP { get; private set; }
    public int? ServerPort { get; private set; }

    public event ClientEventHandler? Connected;
    public event ClientEventHandler? Disconnected;

    public bool IsConnected => logicClient.Connected && !cancellationTokenSource.IsCancellationRequested;

    public void Disconnect() => cancellationTokenSource.Cancel();

    // constructors

    public SlimClient()
    {
        this.logicClient = new();
        this.cancellationTokenSource = new CancellationTokenSource();
    }

    internal SlimClient(TcpClient logicClient, Guid guid, CancellationTokenSource cancellationTokenSource)
    {
        this.logicClient = logicClient;
        this.Guid = guid;
        this.cancellationTokenSource = cancellationTokenSource;
    }

    // conection methods

    public Task Connect(string serverAddress, int serverPort = SlimServer.DefaultServerPort, int timeout = DefaultTimeout)
        => Connect(IPAddress.Parse(serverAddress), serverPort, timeout);

    public async Task Connect(IPAddress serverIP, int serverPort = SlimServer.DefaultServerPort, int timeout = DefaultTimeout)
    {
        var timeoutCancellationToken = new CancellationTokenSource(timeout).Token;
        var connectionCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, timeoutCancellationToken).Token;
        var ipEndPoint = new IPEndPoint(serverIP, serverPort)!;
        await logicClient.ConnectAsync(ipEndPoint, connectionCancellationToken);
        ServerIP = serverIP;
        ServerPort = serverPort;

        StartRunLoop();
    }

    internal void StartRunLoop()
    {
        messagesSemaphore = new SemaphoreSlim(0);
        Connected?.Invoke(this);
        _ = ReceiveRunLoop();
    }

    async Task ReceiveRunLoop()
    {
        var buffer = new byte[1_024];
        string partialMessage = "";
        try
        {
            while (IsConnected)
            {
                int bytesReceived = await logicClient.GetStream().ReadAsync(buffer, cancellationTokenSource.Token);
                if (bytesReceived == 0) break;

                var stringBuffer = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                var stringList = stringBuffer.Split('\0').ToList();
                if (partialMessage != "") stringList[0] = partialMessage + stringList[0];

                partialMessage = stringList.Last();
                stringList.RemoveAt(stringList.Count - 1);

                foreach (var message in stringList)
                {
                    messagesQueue.Enqueue(message);
                    messagesSemaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        cancellationTokenSource.Cancel();
        logicClient.Close();

        Disconnected?.Invoke(this);
    }

    public async Task WriteAsync(string dataString, int? timeout = null)
    {
        CancellationToken cancellationToken;
        if (timeout != null)
        {
            var timeoutCancellationToken = new CancellationTokenSource((int)timeout).Token;
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, timeoutCancellationToken).Token;
        }
        else
        {
            cancellationToken = cancellationTokenSource.Token;
        }

        dataString += "\0";
        var messageBytes = Encoding.UTF8.GetBytes(dataString);
        if (logicClient.Connected) await logicClient.GetStream().WriteAsync(messageBytes, cancellationToken);
    }

    public async Task<string> ReadAsync(int? timeout = null)
    {
        CancellationToken cancellationToken;
        if (timeout != null)
        {
            var timeoutCancellationToken = new CancellationTokenSource((int)timeout).Token;
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, timeoutCancellationToken).Token;
        }
        else
        {
            cancellationToken = cancellationTokenSource.Token;
        }

        await messagesSemaphore.WaitAsync(cancellationToken);
        messagesQueue.TryDequeue(out var result);
        return result!;
    }
}
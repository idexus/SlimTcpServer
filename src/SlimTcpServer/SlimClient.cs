using System;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SlimTcpServer
{
    public class NoConnectionException : Exception { }

    public class SlimClient : IDisposable
    {
        public const int DefaultSendTimeout = 8000;

        // private

        readonly ConcurrentQueue<string> messagesQueue = new ConcurrentQueue<string>();
        Socket client;
        SemaphoreSlim messagesSemaphore = new SemaphoreSlim(0);
        CancellationTokenSource cancellationTokenSource;

        // public

        public Guid Guid { get; private set; }
        public IPAddress ServerIP { get; private set; }
        public int ServerPort { get; private set; }

        public event ClientEventHandler Connected;
        public event ClientEventHandler Disconnected;

        public int SendTimeout { get; set; } = DefaultSendTimeout;

        public bool IsConnected => client.Connected;

        // constructors

        public SlimClient() { }

        internal SlimClient(Socket socket, Guid guid, CancellationTokenSource cancellationTokenSource)
        {
            this.client = socket;
            this.Guid = guid;
            this.cancellationTokenSource = cancellationTokenSource;
        }

        // conection methods

        public Task Connect(string serverAddress, int serverPort = SlimServer.DefaultPort)
            => Connect(IPAddress.Parse(serverAddress), serverPort);

        public async Task Connect(IPAddress serverIP, int serverPort = SlimServer.DefaultPort)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            client = new Socket(SocketType.Stream, ProtocolType.Tcp);
            client.ReceiveTimeout = int.MaxValue;
            await client.ConnectAsync(serverIP, serverPort);
            if (!client.Connected) throw new SocketException();
            ServerIP = serverIP;
            ServerPort = serverPort;
            StartRunLoop();
        }

        Task receiveLoop;

        public Task Disconnect()
        {
            return Task.Run(async () =>
            {
                if (client != null)
                {
                    cancellationTokenSource.Cancel();
                    if (receiveLoop != null) await receiveLoop;
                    if (client.Connected) client.Close();
                    client.Dispose();
                    client = null;
                    receiveLoop = null;
                }
            });
        }

        internal void StartRunLoop()
        {
            messagesSemaphore = new SemaphoreSlim(0);
            Connected?.Invoke(this);
            StartReceiveRunLoop();
        }

        void StartReceiveRunLoop()
        {
            receiveLoop = Task.Run(() =>
            {
                var buffer = new byte[1_024];
                string partialMessage = "";
                try
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested && IsConnected)
                    {
                        var args = new SocketAsyncEventArgs();
                        args.SetBuffer(buffer, 0, 1024);                       
                        args.Wait(client.ReceiveAsync, cancellationTokenSource.Token);
                        int bytesReceived = args.BytesTransferred;
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
                    Debug.WriteLine(ex.Message);
                }

                client.Close();

                Disconnected?.Invoke(this);
            });
        }

        public async Task WriteAsync(string dataString)
        {
            await Task.Run(() =>
            {
                dataString += "\0";
                var messageBytes = Encoding.UTF8.GetBytes(dataString);
                if (client.Connected)
                {
                    client.SendTimeout = SendTimeout;

                    var args = new SocketAsyncEventArgs();
                    args.SetBuffer(messageBytes, 0, messageBytes.Length);

                    args.Wait(client.SendAsync, cancellationTokenSource.Token);                    

                    int sendCount = args.BytesTransferred;
                    if (sendCount != messageBytes.Length) throw new SocketException();
                }
            });
        }

        public async Task<string> ReadAsync()
        {
            await messagesSemaphore.WaitAsync(cancellationTokenSource.Token);
            messagesQueue.TryDequeue(out var result);
            return result;
        }

        public void Dispose()
        {
            ((IDisposable)client).Dispose();
        }
    }
}
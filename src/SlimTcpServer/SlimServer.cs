using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace SlimTcpServer
{
    public class SlimServer : IDisposable
    {
        // private

        readonly ConcurrentDictionary<Guid, SlimClient> clientDictionary = new ConcurrentDictionary<Guid, SlimClient>();
        Socket server;
        CancellationTokenSource cancellationTokenSource;
        Task serverRunTask;

        // public

        public const int DefaultPort = 5095;
        public const int DefaultBackLog = 100;

        public event ServerEventHandler ServerStarted;
        public event ServerEventHandler ServerStopped;
        public event ClientEventHandler ClientConnected;
        public event ClientEventHandler ClientDisconnected;

        public int ServerPort { get; private set; }
        public bool IsRunning { get; private set; }

        public bool IsStopRequested => cancellationTokenSource?.IsCancellationRequested ?? false;

        SemaphoreSlim serverSemaphore = new SemaphoreSlim(1);
        public async Task StartAsync(int serverPort = DefaultPort, int backLog = DefaultBackLog)
        {
            await serverSemaphore.WaitAsync();

            ServerPort = serverPort;
            var ipEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
            server = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipEndPoint);
            server.Listen(100);
            IsRunning = true;
            ServerStarted?.Invoke(this);

            cancellationTokenSource = new CancellationTokenSource();

            serverRunTask = Task.Run(async () =>
            {
                await Task.Factory.StartNew(RunLoop, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                ReleaseResources();

                ServerStopped?.Invoke(this);
                serverSemaphore.Release();
            });
        }

        public async Task StopAsync()
        {
            cancellationTokenSource.Cancel();
            if (serverRunTask != null) await serverRunTask;
        }

        public void ReleaseResources()
        {            
            if (server != null)
            {
                if (server.Connected) server.Disconnect(true);
                server.Close();
                server.Dispose();

                clientDictionary.Clear();
            }
            if (cancellationTokenSource != null) cancellationTokenSource.Dispose();
            server = null;
            cancellationTokenSource = null;
            IsRunning = false;
        }

        void RunLoop()
        {
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {                    
                    var client = new SocketAsyncEventArgs().Wait(server.AcceptAsync, cancellationTokenSource.Token).AcceptSocket;

                    var clientGuid = Guid.NewGuid();
                    var clientSlim = new SlimClient(client, clientGuid, CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token));
                    clientDictionary[clientGuid] = clientSlim;

                    clientSlim.Connected += e => ClientConnected?.Invoke(e);
                    clientSlim.Disconnected += e =>
                    {
                        clientDictionary.TryRemove(clientGuid, out _);
                        ClientDisconnected?.Invoke(e);
                    };

                    clientSlim.StartRunLoop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel(true);
            ReleaseResources();
        }
    }
}
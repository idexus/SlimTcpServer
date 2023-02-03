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

        // public

        public const int DefaultPort = 5095;
        public const int DefaultBackLog = 100;

        public event ServerEventHandler ServerStarted;
        public event ServerEventHandler ServerStopped;
        public event ClientEventHandler ClientConnected;
        public event ClientEventHandler ClientDisconnected;

        public int ServerPort { get; private set; }
        public bool IsRunning { get; private set; }

        public bool IsStopRequested => cancellationTokenSource.IsCancellationRequested;

        public void Stop() => cancellationTokenSource.Cancel(true);

        SemaphoreSlim serverSemaphore = new SemaphoreSlim(1);
        public void Start(int serverPort = DefaultPort, int backLog = DefaultBackLog)
        {
            ServerPort = serverPort;
            Task.Run(async () =>
            {
                await serverSemaphore.WaitAsync();

                var ipEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
                server = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(ipEndPoint);
                server.Listen(100);
                IsRunning = true;
                ServerStarted?.Invoke(this);

                cancellationTokenSource = new CancellationTokenSource();

                await Task.Factory.StartNew(RunLoop, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                ReleaseResources();

                ServerStopped?.Invoke(this);
                serverSemaphore.Release();
            });
        }

        public void ReleaseResources()
        {            
            if (server != null)
            {
                server.Close();

                clientDictionary.Clear();
                ((IDisposable)server).Dispose();
            }
            if (cancellationTokenSource != null) ((IDisposable)cancellationTokenSource).Dispose();
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
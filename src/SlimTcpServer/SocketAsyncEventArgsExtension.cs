using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SlimTcpServer
{
	public static class SocketAsyncEventArgsExtension
	{
        public static SocketAsyncEventArgs Wait(this SocketAsyncEventArgs args, Func<SocketAsyncEventArgs, bool> func, CancellationToken cancellationToken)
        {
            args = args ?? new SocketAsyncEventArgs();
            var finished = new TaskCompletionSource<SocketAsyncEventArgs>();
            args.Completed += (sender, complArgs) =>
            {
                finished.SetResult(complArgs);
            };
            var completedAsync = func(args);
            if (!completedAsync) finished.SetResult(args);
            Task.WaitAny(new Task[] { finished.Task }, cancellationToken);
            return finished.Task.Result;
        }
    }
}
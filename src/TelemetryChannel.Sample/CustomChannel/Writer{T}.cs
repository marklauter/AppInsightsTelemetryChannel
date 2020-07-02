using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TelemetryChannel.Sample.CustomChannel
{
    internal abstract class Writer<T> : IWriter<T>, IDisposable
    {
        private CancellationTokenSource cancellationTokenSource = null;

        protected bool IsRunning { get; set; } = false;
        protected ConcurrentQueue<T> Messages { get; } = new ConcurrentQueue<T>();

        public Writer()
        {
            StartAsync();
        }

        public void Write(T message)
        {
            Messages.Enqueue(message);
        }

        protected async void StartAsync()
        {
            if (cancellationTokenSource == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
                await Task.Run(() => Listen(cancellationTokenSource.Token));
            }
        }

        protected abstract void Listen(CancellationToken cancellationToken);

        public void Dispose()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel(false);
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            var wait = new SpinWait();
            while (IsRunning)
            {
                wait.SpinOnce();
            }

            Flush();
        }

        public abstract void Flush();
    }
}
using Microsoft.ApplicationInsights.Channel;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelemetryChannel.Sample.CustomChannel
{
    internal sealed class FileWriter : Writer<ITelemetry>
    {
        private string _fileName;
        private readonly object _gate = new object();

        private void SetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path == _fileName)
            {
                return;
            }

            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _fileName = path;
            RenameFile();
        }

        public string FileName
        {
            get => _fileName;
            set => SetPath(value);
        }

        public override void Flush()
        {
            if (Messages.IsEmpty || !IsRunning)
            {
                return;
            }

            RenameFile();

            lock (_gate)
            {
                using (var stream = new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read, 4096 * 128, FileOptions.SequentialScan | FileOptions.WriteThrough))
                {
                    while (Messages.TryDequeue(out var telemetry))
                    {
                        var message = telemetry.ToLogText();
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            var data = Encoding.UTF8.GetBytes(message);
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
        }

        protected override async void Listen(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            // wait for filename to be set
            var wait = default(SpinWait);
            while (FileName == null && !cancellationToken.IsCancellationRequested)
            {
                wait.SpinOnce();
            }

            IsRunning = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                Flush();
                wait.SpinOnce();
                await Task.Delay(5000, cancellationToken);
            }

            IsRunning = false;
        }

        private void RenameFile()
        {
            if (File.Exists(_fileName))
            {
                lock (_gate)
                {
                    var fileInfo = new FileInfo(_fileName);
                    fileInfo.Refresh();

                    if (DateTime.Today != fileInfo.CreationTime.Date)
                    {
                        var newName = $"{_fileName}.{fileInfo.CreationTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";
                        var i = 0;
                        while (File.Exists(newName))
                        {
                            newName = $"{_fileName}.{fileInfo.CreationTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}({++i})";
                        }
                        File.Copy(_fileName, newName);
                        File.Delete(_fileName);
                    }
                }
            }
        }
    }
}
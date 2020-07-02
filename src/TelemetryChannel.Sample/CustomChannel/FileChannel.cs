using Microsoft.ApplicationInsights.Channel;
using System.IO;

namespace TelemetryChannel.Sample.CustomChannel
{
    public sealed class FileChannel : ITelemetryChannel
    {
        private readonly FileWriter fileWriter = new FileWriter();

        public bool? DeveloperMode { get; set; }

        public string EndpointAddress { get; set; }

        public string FileName
        {
            get => Path.GetDirectoryName(fileWriter.FileName);
            set => fileWriter.FileName = value;
        }

        public void Flush()
        {
            fileWriter.Flush();
        }

        public void Send(ITelemetry item)
        {
            fileWriter.Write(item);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    fileWriter.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
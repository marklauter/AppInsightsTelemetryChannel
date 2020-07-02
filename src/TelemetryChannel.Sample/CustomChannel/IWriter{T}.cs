namespace TelemetryChannel.Sample.CustomChannel
{
    internal interface IWriter<T>
    {
        void Write(T message);

        void Flush();
    }
}

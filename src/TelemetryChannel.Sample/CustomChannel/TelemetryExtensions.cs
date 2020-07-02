using Microsoft.ApplicationInsights.Channel;
using Newtonsoft.Json;
using System.Text;

namespace TelemetryChannel.Sample.CustomChannel
{
    internal static class TelemetryExtensions
    {
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        public static string ToLogText(this ITelemetry telemetry)
        {
            telemetry.Sanitize();

            return telemetry is Microsoft.ApplicationInsights.DataContracts.TraceTelemetry
                ? (telemetry as Microsoft.ApplicationInsights.DataContracts.TraceTelemetry).ToLogText()
                : telemetry is Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry
                    ? (telemetry as Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry).ToLogText()
                    : string.Empty;
        }

        public static string ToLogText(this Microsoft.ApplicationInsights.DataContracts.TraceTelemetry telemetry)
        {
            var builder = new StringBuilder($"{telemetry.Timestamp:o} INFO ");
            builder.Append($"[{telemetry.Context.Cloud.RoleInstance}], ");
            builder.Append($"Message='{telemetry.Message}', ");
            builder.AppendLine($"Telemetry: {JsonConvert.SerializeObject(telemetry, serializerSettings)}");
            return builder.ToString();
        }

        public static string ToLogText(this Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry telemetry)
        {
            var builder = new StringBuilder($"{telemetry.Timestamp:o} ERROR ");
            builder.Append($"[{telemetry.Context.Cloud.RoleInstance}], ");
            if (!string.IsNullOrEmpty(telemetry.Message))
            {
                builder.Append($"Message='{telemetry.Message}', ");
            }

            builder.Append($"{telemetry.Exception.GetType().FullName}='{telemetry.Exception.Message}', ");
            builder.AppendLine($"Telemetry: {JsonConvert.SerializeObject(telemetry, serializerSettings)}");

            var innerException = telemetry.Exception.InnerException;
            while (innerException != null)
            {
                builder.AppendLine($"{innerException.GetType().FullName}: {innerException.Message}");
                innerException = innerException.InnerException;
            }

            builder.AppendLine(telemetry.Exception.StackTrace);

            return builder.ToString();
        }
    }
}
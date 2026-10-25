namespace DocumentService.Api.Observability
{
    using System.Diagnostics; 
    using Microsoft.Extensions.Hosting.Observability;

    public static class ToolTelemetry
    {
        public static readonly ActivitySource Source =
            new(TelemetryConstants.DocumentServiceSourceName, TelemetryConstants.TelemetryVersion);
    }
}

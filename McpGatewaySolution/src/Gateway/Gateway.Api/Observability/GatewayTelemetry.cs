namespace Gateway.Api.Observability
{
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    using Microsoft.Extensions.Hosting.Observability;

    public static class GatewayTelemetry
    {
        public static readonly ActivitySource Source =
            new(TelemetryConstants.GatewaySourceName, TelemetryConstants.TelemetryVersion);

        public static readonly Meter Meter =
            new(TelemetryConstants.GatewayMeterName, TelemetryConstants.TelemetryVersion);

        public static readonly Histogram<double> ToolDuration =
            Meter.CreateHistogram<double>(
                TelemetryConstants.ToolDurationMetric,
                unit: "ms",
                description: "MCP tool execution duration");

        public static readonly Counter<long> ToolDeniedCounter =
            Meter.CreateCounter<long>(
                TelemetryConstants.ToolDeniedMetric,
                description: "Number of denied MCP tool calls");

        public static readonly Counter<long> ToolErrorCounter =
            Meter.CreateCounter<long>(
                TelemetryConstants.ToolErrorMetric,
                description: "Number of MCP tool errors");
    }
}
namespace Agent.Api.Observability
{
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    using Microsoft.Extensions.Hosting.Observability;
  
    public static class AgentTelemetry
    {
        public static readonly ActivitySource Source =
            new(TelemetryConstants.AgentSourceName, TelemetryConstants.TelemetryVersion);

        public static readonly Meter Meter =
            new(TelemetryConstants.AgentMeterName, TelemetryConstants.TelemetryVersion);

        public static readonly Counter<long> TokenCounter =
            Meter.CreateCounter<long>(
                TelemetryConstants.TokenUsageMetric,
                unit: "tokens",
                description: "Number of tokens used by LLM");

        public static readonly Histogram<double> ChatDuration =
            Meter.CreateHistogram<double>(
                TelemetryConstants.ChatDurationMetric,
                unit: "ms",
                description: "LLM chat duration");

        public static readonly Histogram<long> ToolCallsPerInvocation =
            Meter.CreateHistogram<long>(
                TelemetryConstants.ToolCallsPerInvocationMetric,
                unit: "calls",
                description: "Tool calls per agent invocation");
    }
}

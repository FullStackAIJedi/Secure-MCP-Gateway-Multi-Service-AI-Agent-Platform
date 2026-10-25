namespace Gateway.Api.Observability
{
    using System.Diagnostics; 
    using Microsoft.Extensions.Hosting.Observability;

    public static class McpTelemetryExtensions
    {
        public static void RecordToolSuccess(
            this Activity? activity, string toolName, string scope, double ms)
        {
            GatewayTelemetry.ToolDuration.Record(
                ms,
                new KeyValuePair<string, object?>("mcp.tool.name", toolName),
                new KeyValuePair<string, object?>("mcp.tool.scope", scope),
                new KeyValuePair<string, object?>("outcome", "success"));

            activity?.SetTag("outcome", "success");
        }

        public static void RecordToolDenied(
            this Activity? activity, string toolName, string scope)
        {
            GatewayTelemetry.ToolDeniedCounter.Add(
                1,
                new KeyValuePair<string, object?>("mcp.tool.name", toolName),
                new KeyValuePair<string, object?>("mcp.tool.scope", scope));

            activity?.SetTag("outcome", "denied");
        }

        public static void RecordToolError(
            this Activity? activity, string toolName, string scope, Exception ex, double ms)
        {
            GatewayTelemetry.ToolErrorCounter.Add(
                1,
                new KeyValuePair<string, object?>("mcp.tool.name", toolName),
                new KeyValuePair<string, object?>("mcp.tool.scope", scope),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

            activity?.SetTag("outcome", "error");
            activity?.SetError(ex);
        }
    }
}

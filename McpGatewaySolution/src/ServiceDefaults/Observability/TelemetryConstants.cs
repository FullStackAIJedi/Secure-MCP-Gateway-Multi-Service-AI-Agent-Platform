namespace Microsoft.Extensions.Hosting.Observability
{
    public static class TelemetryConstants
    {
        public const string AgentSourceName = "Agent.LLM";
        public const string GatewaySourceName = "Gateway.MCP";
        public const string TaskServiceSourceName = "TaskService.Tools";
        public const string DocumentServiceSourceName = "DocumentService.Tools";

        public const string AgentMeterName = "Agent.LLM.Metrics";
        public const string GatewayMeterName = "Gateway.MCP.Metrics";
        public const string TaskServiceMeterName = "TaskService.Tools.Metrics";
        public const string DocumentServiceMeterName = "DocumentService.Tools.Metrics";

        public const string TokenUsageMetric = "gen_ai.client.token.usage";
        public const string ChatDurationMetric = "gen_ai.client.chat.duration";
        public const string ToolCallsPerInvocationMetric = "gen_ai.invoke_agent.tool_calls";
        public const string ToolDurationMetric = "mcp.tool.execution.duration";
        public const string ToolDeniedMetric = "mcp.tool.denied.count";
        public const string ToolErrorMetric = "mcp.tool.error.count";

        public const string TelemetryVersion = "1.0.0";
    }
}

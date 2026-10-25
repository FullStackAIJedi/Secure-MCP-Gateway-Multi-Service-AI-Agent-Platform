namespace Agent.Api.Observability
{
    using System.Diagnostics; 
    using Microsoft.Extensions.Hosting.Observability;

    public static class LlmTelemetryExtensions
    {
        public static void RecordTokenUsage(
            this Activity? activity, string model, int? inputTokens, int? outputTokens)
        {
            if (inputTokens.HasValue && inputTokens.Value > 0)
            {
                AgentTelemetry.TokenCounter.Add(
                    inputTokens.Value,
                    new KeyValuePair<string, object?>("gen_ai.token.type", "input"),
                    new KeyValuePair<string, object?>("gen_ai.request.model", model));
            }

            if (outputTokens.HasValue && outputTokens.Value > 0)
            {
                AgentTelemetry.TokenCounter.Add(
                    outputTokens.Value,
                    new KeyValuePair<string, object?>("gen_ai.token.type", "output"),
                    new KeyValuePair<string, object?>("gen_ai.request.model", model));
            }

            activity?.AddGenAiTokenUsage(inputTokens, outputTokens);
        }

        public static void RecordChatDuration(
            this Activity? activity, string model, double ms)
        {
            AgentTelemetry.ChatDuration.Record(
                ms,
                new KeyValuePair<string, object?>("gen_ai.request.model", model));
        }

        public static void RecordToolCalls(
            this Activity? activity, int toolCount)
        {
            AgentTelemetry.ToolCallsPerInvocation.Record(toolCount);
        }
    }
}

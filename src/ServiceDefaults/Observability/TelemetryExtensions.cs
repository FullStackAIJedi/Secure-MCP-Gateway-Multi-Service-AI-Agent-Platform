namespace Microsoft.Extensions.Hosting.Observability
{
    using System.Diagnostics;

    public static class TelemetryExtensions
    {
        public static void AddGenAiChatAttributes(this Activity activity, string model)
        {
            activity.SetTag(GenAiAttributes.System, GenAiAttributes.SystemOllama);
            activity.SetTag(GenAiAttributes.OperationName, GenAiAttributes.OperationChat);
            activity.SetTag(GenAiAttributes.RequestModel, model);
        }

        public static void AddGenAiTokenUsage(
            this Activity activity, int? inputTokens, int? outputTokens)
        {
            if (inputTokens.HasValue)
                activity.SetTag(GenAiAttributes.UsageInputTokens, inputTokens.Value);

            if (outputTokens.HasValue)
                activity.SetTag(GenAiAttributes.UsageOutputTokens, outputTokens.Value);
        }

        public static void AddMcpToolAttributes(
            this Activity activity, string toolName, string scope, string subjectId)
        {
            activity.SetTag(McpAttributes.MethodName, McpAttributes.MethodToolsCall);
            activity.SetTag(McpAttributes.ToolName, toolName);
            activity.SetTag(McpAttributes.ToolScope, scope);
            activity.SetTag(McpAttributes.SubjectId, subjectId);
        }

        public static void SetError(this Activity activity, Exception ex)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.SetTag("error.type", ex.GetType().Name);
            activity.SetTag("error.message", ex.Message);
        }
    }
}
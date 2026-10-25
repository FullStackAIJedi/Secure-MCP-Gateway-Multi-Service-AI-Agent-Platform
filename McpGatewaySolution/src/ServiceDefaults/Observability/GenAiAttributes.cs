namespace Microsoft.Extensions.Hosting.Observability
{
    public static class GenAiAttributes
    {
        public const string System = "gen_ai.system";
        public const string RequestModel = "gen_ai.request.model";
        public const string ResponseModel = "gen_ai.response.model";
        public const string UsageInputTokens = "gen_ai.usage.input_tokens";
        public const string UsageOutputTokens = "gen_ai.usage.output_tokens";
        public const string OperationName = "gen_ai.operation.name";
        public const string ResponseFinishReasons = "gen_ai.response.finish_reasons";
        public const string ToolName = "gen_ai.tool.name";
        public const string ToolCallId = "gen_ai.tool.call.id";

        public const string OperationChat = "chat";
        public const string OperationExecuteTool = "execute_tool";
        public const string SystemOllama = "ollama";
    }
}

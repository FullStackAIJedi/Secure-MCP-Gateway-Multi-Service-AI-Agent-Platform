namespace Microsoft.Extensions.Hosting.Observability
{
    public static class McpAttributes
    {
        public const string MethodName = "mcp.method.name";
        public const string SessionId = "mcp.session.id";
        public const string ProtocolVersion = "mcp.protocol.version";
        public const string ToolName = "mcp.tool.name";
        public const string ToolScope = "mcp.tool.scope";
        public const string SubjectId = "mcp.subject.id";

        public const string MethodToolsCall = "tools/call";
        public const string MethodToolsList = "tools/list";
        public const string MethodInitialize = "initialize";
    }
}

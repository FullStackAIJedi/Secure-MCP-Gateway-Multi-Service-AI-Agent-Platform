namespace Gateway.Core.Policies
{
    using Gateway.Core.Abstractions;
    using Gateway.Core.Scopes;

    public sealed class ToolRegistry : IToolRegistry
    {
        private static readonly Dictionary<string, ToolScope> Tools = new(StringComparer.Ordinal)
        {
            ["list_tasks"] = ToolScope.Read,
            ["get_task"] = ToolScope.Read,
            ["search_tasks"] = ToolScope.Read,
            ["list_documents"] = ToolScope.Read,
            ["search_documents"] = ToolScope.Read,
            ["get_document"] = ToolScope.Read,

            ["create_task"] = ToolScope.Write,
            ["complete_task"] = ToolScope.Write,
            ["add_document"] = ToolScope.Write,
            ["update_document"] = ToolScope.Write,

            ["delete_task"] = ToolScope.Admin,
            //["delete_task_by_id"] = ToolScope.Admin,
            ["delete_document"] = ToolScope.Admin,
            //["delete_document_by_id"] = ToolScope.Admin
        };

        public IReadOnlyDictionary<string, ToolScope> AllTools => Tools;

        public bool TryGetScope(string toolName, out ToolScope scope)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                scope = default;
                return false;
            }

            return Tools.TryGetValue(toolName, out scope);
        }
    }
}

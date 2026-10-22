namespace Gateway.Core.Policies
{
    using Gateway.Core.Abstractions;

    //public sealed class ScopeBasedToolPermissionPolicy : IToolPermissionPolicy
    //{
    //    private static readonly Dictionary<string, HashSet<string>> ScopeToolMap = new()
    //    {
    //        ["mcp.tools.read"] = ["list_tasks", "get_task", "search_tasks", "search_documents", "get_document"],
    //        ["mcp.tools.write"] = ["create_task", "complete_task", "add_document"],
    //        ["mcp.tools.admin"] = ["delete_task", "delete_document"]
    //    };

    //    public Task<bool> IsAllowedAsync(
    //        string subjectId,
    //        string toolName,
    //        string downstreamService,
    //        CancellationToken cancellationToken)
    //    {
    //        var allowed = ScopeToolMap.Values.Any(set => set.Contains(toolName));
    //        return Task.FromResult(allowed);
    //    }
    //}
    public sealed class ScopeBasedToolPermissionPolicy : IToolPermissionPolicy
    {
        private static readonly Dictionary<string, HashSet<string>> ScopeToolMap = new()
        {
            ["mcp.tools.read"] =
                ["list_tasks", "get_task", "search_tasks", "list_documents", "search_documents", "get_document"],
            ["mcp.tools.write"] = ["create_task", "complete_task", "add_document", "update_document"],
            ["mcp.tools.admin"] = ["delete_task", "delete_document"]
        };

        public Task<bool> IsAllowedAsync(
            string subjectId,
            string toolName,
            string downstreamService,
            CancellationToken cancellationToken)
        {
            var allowed = ScopeToolMap.Values.Any(set => set.Contains(toolName));
            return Task.FromResult(allowed);
        }
    }
}
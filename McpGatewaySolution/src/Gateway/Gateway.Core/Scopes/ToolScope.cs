namespace Gateway.Core.Scopes
{
    public enum ToolScope
    {
        Read = 0,
        Write = 1,
        Admin = 2
    }

    public static class ToolScopeExtensions
    {
        public static string ToClaimValue(this ToolScope scope) => scope switch
        {
            ToolScope.Read => "mcp.tools.read",
            ToolScope.Write => "mcp.tools.write",
            ToolScope.Admin => "mcp.tools.admin",
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };

        public static ToolScope? FromClaimValue(string? claim)
        {
            return claim switch
            {
                "mcp.tools.read" => ToolScope.Read,
                "mcp.tools.write" => ToolScope.Write,
                "mcp.tools.admin" => ToolScope.Admin,
                _ => null
            };
        }
    }
}

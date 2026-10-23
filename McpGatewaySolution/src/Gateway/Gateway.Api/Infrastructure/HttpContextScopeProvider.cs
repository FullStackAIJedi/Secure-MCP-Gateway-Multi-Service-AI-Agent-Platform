namespace Gateway.Api.Infrastructure
{
    using Gateway.Core.Abstractions;
    using Gateway.Core.Scopes;

    public sealed class HttpContextScopeProvider(IHttpContextAccessor accessor) : IScopeProvider
    {
        public IReadOnlySet<ToolScope> GetScopes()
        {
            var scopes = new HashSet<ToolScope>();
            var user = accessor.HttpContext?.User;
            if (user is null) return scopes;

            foreach (var claim in user.FindAll("scope"))
            {
                foreach (var scopeStr in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var scope = ToolScopeExtensions.FromClaimValue(scopeStr);
                    if (scope.HasValue) scopes.Add(scope.Value);
                }
            }

            return scopes;
        }
    }
}

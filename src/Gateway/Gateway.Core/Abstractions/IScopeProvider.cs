namespace Gateway.Core.Abstractions
{
    using Gateway.Core.Scopes;

    public interface IScopeProvider
    {
        IReadOnlySet<ToolScope> GetScopes();
    }
}

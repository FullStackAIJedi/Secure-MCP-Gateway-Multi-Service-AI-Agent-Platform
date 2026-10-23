namespace Gateway.Core.Abstractions
{
    using Gateway.Core.Scopes;

    public interface IToolRegistry
    {
        bool TryGetScope(string toolName, out ToolScope scope);
        IReadOnlyDictionary<string, ToolScope> AllTools { get; }
    }
}

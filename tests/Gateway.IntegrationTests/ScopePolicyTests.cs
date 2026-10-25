namespace Gateway.IntegrationTests
{
    using Xunit;
    using FluentAssertions;

    using Gateway.Core.Policies;
    using Gateway.Core.Scopes;
 
    public sealed class ScopePolicyTests
    {
        private readonly ScopeBasedToolPermissionPolicy _policy = new(new ToolRegistry());

        private static IReadOnlySet<ToolScope> Scopes(params ToolScope[] scopes)
        {
            IReadOnlySet<ToolScope> set = new HashSet<ToolScope>(scopes);
            return set;
        }

        private static readonly IReadOnlySet<ToolScope> AllScopes =
            Scopes(ToolScope.Read, ToolScope.Write, ToolScope.Admin);

        [Theory]
        [InlineData("list_tasks")]
        [InlineData("get_task")]
        [InlineData("search_tasks")]
        [InlineData("list_documents")]
        [InlineData("search_documents")]
        [InlineData("get_document")]
        public async Task ReadToolsAreAllowed(string toolName)
        {
            var allowed = await _policy.IsAllowedAsync("user", toolName, AllScopes, default);
            allowed.Should().BeTrue();
        }

        [Theory]
        [InlineData("create_task")]
        [InlineData("complete_task")]
        [InlineData("add_document")]
        [InlineData("update_document")]
        public async Task WriteToolsAreAllowed(string toolName)
        {
            var allowed = await _policy.IsAllowedAsync("user", toolName, AllScopes, default);
            allowed.Should().BeTrue();
        }

        [Theory]
        [InlineData("delete_task")]
        [InlineData("delete_document")]
        public async Task AdminToolsAreAllowed(string toolName)
        {
            var allowed = await _policy.IsAllowedAsync("user", toolName, AllScopes, default);
            allowed.Should().BeTrue();
        }

        [Fact]
        public async Task UnknownToolIsDenied()
        {
            var allowed = await _policy.IsAllowedAsync("user", "nonexistent_tool", AllScopes, default);
            allowed.Should().BeFalse();
        }

        [Fact]
        public async Task ReadToolDeniedWithoutReadScope()
        {
            var allowed = await _policy.IsAllowedAsync(
                "user", "list_tasks", Scopes(ToolScope.Write, ToolScope.Admin), default);
            allowed.Should().BeFalse();
        }

        [Fact]
        public async Task WriteToolDeniedWithoutWriteScope()
        {
            var allowed = await _policy.IsAllowedAsync(
                "user", "create_task", Scopes(ToolScope.Read), default);
            allowed.Should().BeFalse();
        }

        [Fact]
        public async Task AdminToolDeniedWithoutAdminScope()
        {
            var allowed = await _policy.IsAllowedAsync(
                "user", "delete_task", Scopes(ToolScope.Read, ToolScope.Write), default);
            allowed.Should().BeFalse();
        }

        [Fact]
        public async Task NoScopesDeniesEverything()
        {
            var empty = Scopes();

            (await _policy.IsAllowedAsync("user", "list_tasks", empty, default)).Should().BeFalse();
            (await _policy.IsAllowedAsync("user", "create_task", empty, default)).Should().BeFalse();
            (await _policy.IsAllowedAsync("user", "delete_task", empty, default)).Should().BeFalse();
        }
    }
}

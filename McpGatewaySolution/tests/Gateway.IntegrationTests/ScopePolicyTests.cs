namespace Gateway.IntegrationTests
{
    using Gateway.Core.Policies;
    using Xunit;
    using FluentAssertions;

    public sealed class ScopePolicyTests
    {
        private readonly ScopeBasedToolPermissionPolicy _policy = new();

        [Theory]
        [InlineData("list_tasks")]
        [InlineData("get_task")]
        [InlineData("search_tasks")]
        [InlineData("list_documents")]
        [InlineData("search_documents")]
        [InlineData("get_document")]
        public async Task ReadToolsAreAllowed(string toolName)
        {
            var allowed = await _policy.IsAllowedAsync("user", toolName, "any", default);
            allowed.Should().BeTrue();
        }

        [Theory]
        [InlineData("create_task")]
        [InlineData("complete_task")]
        [InlineData("add_document")]
        [InlineData("update_document")]
        public async Task WriteToolsAreAllowed(string toolName)
        {
            var allowed = await _policy.IsAllowedAsync("user", toolName, "any", default);
            allowed.Should().BeTrue();
        }

        [Theory]
        [InlineData("delete_task")]
        [InlineData("delete_document")]
        public async Task AdminToolsAreAllowed(string toolName)
        {
            var allowed = await _policy.IsAllowedAsync("user", toolName, "any", default);
            allowed.Should().BeTrue();
        }

        [Fact]
        public async Task UnknownToolIsDenied()
        {
            var allowed = await _policy.IsAllowedAsync("user", "nonexistent_tool", "any", default);
            allowed.Should().BeFalse();
        }
    }
}

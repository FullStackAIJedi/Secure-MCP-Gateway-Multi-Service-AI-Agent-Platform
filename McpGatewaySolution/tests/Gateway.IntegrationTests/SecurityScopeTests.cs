namespace Gateway.IntegrationTests
{
    using Xunit;
    using FluentAssertions;

    using Gateway.Core.Policies;

    public sealed class SecurityScopeTests
    {
        private readonly ScopeBasedToolPermissionPolicy _policy = new();

        [Fact]
        public async Task ReadToolsAreAllowed()
        {
            (await _policy.IsAllowedAsync("u", "list_tasks", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "get_task", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "search_tasks", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "list_documents", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "search_documents", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "get_document", "any", default)).Should().BeTrue();
        }

        [Fact]
        public async Task WriteToolsAreAllowed()
        {
            (await _policy.IsAllowedAsync("u", "create_task", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "complete_task", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "add_document", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "update_document", "any", default)).Should().BeTrue();
        }

        [Fact]
        public async Task AdminToolsAreAllowed()
        {
            (await _policy.IsAllowedAsync("u", "delete_task", "any", default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "delete_document", "any", default)).Should().BeTrue();
        }

        [Fact]
        public async Task UnknownToolIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "nonexistent", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task EmptyToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task WhitespaceToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "   ", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task NullToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", null!, "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task UppercaseToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "LIST_TASKS", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task MixedCaseToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "List_Tasks", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task SqlInjectionToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "'; DROP TABLE Tasks; --", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task PathTraversalToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "../../etc/passwd", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task XssToolNameIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "<script>alert(1)</script>", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithPrefixIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "mcp.tools.read.list_tasks", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithSuffixIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "list_tasks_admin", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithLeadingSpaceIsDenied()
        {
            (await _policy.IsAllowedAsync("u", " list_tasks", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithTrailingSpaceIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "list_tasks ", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithNewlineIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "list_tasks\n", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithTabIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "list_tasks\t", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithNullByteIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "list_tasks\0", "any", default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithUnicodeLookalikeIsDenied()
        {
            (await _policy.IsAllowedAsync("u", "lіst_tasks", "any", default)).Should().BeFalse();
        }
    }
}
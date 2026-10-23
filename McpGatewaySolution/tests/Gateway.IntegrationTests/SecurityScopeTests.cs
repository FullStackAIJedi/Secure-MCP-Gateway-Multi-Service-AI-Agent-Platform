namespace Gateway.IntegrationTests
{
    using System.Collections;
    using System.Collections.Immutable;

    using Xunit;
    using FluentAssertions;

    using Gateway.Core.Policies;
    using Gateway.Core.Scopes;

    public sealed class SecurityScopeTests
    {
        private readonly ScopeBasedToolPermissionPolicy _policy = new(new ToolRegistry());



        private static ImmutableHashSet<ToolScope> AllScopes()
        {
            return ImmutableHashSet.Create(ToolScope.Read, ToolScope.Write, ToolScope.Admin);
        }

        private static ImmutableHashSet<ToolScope> EmptyScopes()
        {
            return ImmutableHashSet<ToolScope>.Empty;
        }

        private sealed class ReadOnlySet<T>(IEnumerable<T> items) : IReadOnlySet<T>
        {
            private readonly HashSet<T> _set = new(items);

            public int Count => _set.Count;
            public bool Contains(T item) => _set.Contains(item);
            public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);
            public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);
            public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Fact]
        public async Task ReadToolsAreAllowed()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "list_tasks", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "get_task", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "search_tasks", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "list_documents", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "search_documents", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "get_document", all, default)).Should().BeTrue();
        }

        [Fact]
        public async Task WriteToolsAreAllowed()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "create_task", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "complete_task", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "add_document", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "update_document", all, default)).Should().BeTrue();
        }

        [Fact]
        public async Task AdminToolsAreAllowed()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "delete_task", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "delete_document", all, default)).Should().BeTrue();
        }

        [Fact]
        public async Task UnknownToolIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "nonexistent", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task EmptyToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task WhitespaceToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "   ", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task NullToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", null!, all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task UppercaseToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "LIST_TASKS", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task MixedCaseToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "List_Tasks", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task SqlInjectionToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "'; DROP TABLE Tasks; --", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task PathTraversalToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "../../etc/passwd", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task XssToolNameIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "<script>alert(1)</script>", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithPrefixIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "mcp.tools.read.list_tasks", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithSuffixIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "list_tasks_admin", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithLeadingSpaceIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", " list_tasks", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithTrailingSpaceIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "list_tasks ", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithNewlineIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "list_tasks\n", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithTabIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "list_tasks\t", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithNullByteIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "list_tasks\0", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task ToolNameWithUnicodeLookalikeIsDenied()
        {
            var all = AllScopes();
            (await _policy.IsAllowedAsync("u", "lіst_tasks", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task EmptyScopesDenyEverything()
        {
            var empty = EmptyScopes();
            (await _policy.IsAllowedAsync("u", "list_tasks", empty, default)).Should().BeFalse();
            (await _policy.IsAllowedAsync("u", "create_task", empty, default)).Should().BeFalse();
            (await _policy.IsAllowedAsync("u", "delete_task", empty, default)).Should().BeFalse();
        }
    }
}
namespace Gateway.IntegrationTests
{
    using System.Collections.Immutable;

    using Xunit;
    using FluentAssertions;

    using Gateway.Core.Policies;
    using Gateway.Core.Scopes;
 
    public sealed class UnitScopePolicyTests
    {
        private readonly ScopeBasedToolPermissionPolicy _policy =
            new(new ToolRegistry());

        private static ImmutableHashSet<ToolScope> Scopes(params ToolScope[] scopes)
        {
            return ImmutableHashSet.CreateRange(scopes);
        }

        [Fact]
        public async Task ReadUserCanCallReadTool()
        {
            (await _policy.IsAllowedAsync("u", "list_tasks", Scopes(ToolScope.Read), default))
                .Should().BeTrue();
        }

        [Fact]
        public async Task ReadUserCannotCallWriteTool()
        {
            (await _policy.IsAllowedAsync("u", "create_task", Scopes(ToolScope.Read), default))
                .Should().BeFalse();
        }

        [Fact]
        public async Task ReadUserCannotCallAdminTool()
        {
            (await _policy.IsAllowedAsync("u", "delete_task", Scopes(ToolScope.Read), default))
                .Should().BeFalse();
        } 

        [Fact]
        public async Task WriteUserCanCallWriteTool()
        {
            (await _policy.IsAllowedAsync("u", "create_task", Scopes(ToolScope.Write), default))
                .Should().BeTrue();
        }

        [Fact]
        public async Task WriteUserCannotCallAdminTool()
        {
            (await _policy.IsAllowedAsync("u", "delete_task", Scopes(ToolScope.Write), default))
                .Should().BeFalse();
        } 

        [Fact]
        public async Task AdminUserCanCallAdminTool()
        {
            (await _policy.IsAllowedAsync("u", "delete_task", Scopes(ToolScope.Admin), default))
                .Should().BeTrue();
        }

        [Fact]
        public async Task UserWithAllScopesCanCallEverything()
        {
            var all = Scopes(ToolScope.Read, ToolScope.Write, ToolScope.Admin);

            (await _policy.IsAllowedAsync("u", "list_tasks", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "create_task", all, default)).Should().BeTrue();
            (await _policy.IsAllowedAsync("u", "delete_task", all, default)).Should().BeTrue();
        }

        [Fact]
        public async Task UserWithNoScopesCannotCallAnything()
        {
            var empty = Scopes();

            (await _policy.IsAllowedAsync("u", "list_tasks", empty, default)).Should().BeFalse();
            (await _policy.IsAllowedAsync("u", "create_task", empty, default)).Should().BeFalse();
            (await _policy.IsAllowedAsync("u", "delete_task", empty, default)).Should().BeFalse();
        }

        [Fact]
        public async Task UnknownToolDeniedEvenWithAllScopes()
        {
            var all = Scopes(ToolScope.Read, ToolScope.Write, ToolScope.Admin);
            (await _policy.IsAllowedAsync("u", "unknown", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task EmptyToolDeniedEvenWithAllScopes()
        {
            var all = Scopes(ToolScope.Read, ToolScope.Write, ToolScope.Admin);
            (await _policy.IsAllowedAsync("u", "", all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task NullToolDeniedEvenWithAllScopes()
        {
            var all = Scopes(ToolScope.Read, ToolScope.Write, ToolScope.Admin);
            (await _policy.IsAllowedAsync("u", null!, all, default)).Should().BeFalse();
        }

        [Fact]
        public async Task EmptySubjectDenied()
        {
            (await _policy.IsAllowedAsync("", "list_tasks", Scopes(ToolScope.Read), default))
                .Should().BeFalse();
        }

        [Fact]
        public async Task NullSubjectDenied()
        {
            (await _policy.IsAllowedAsync(null!, "list_tasks", Scopes(ToolScope.Read), default))
                .Should().BeFalse();
        }

        [Fact]
        public async Task WhitespaceSubjectDenied()
        {
            (await _policy.IsAllowedAsync("   ", "list_tasks", Scopes(ToolScope.Read), default))
                .Should().BeFalse();
        }

        [Theory]
        [InlineData("LIST_TASKS")]
        [InlineData("List_Tasks")]
        [InlineData(" list_tasks")]
        [InlineData("list_tasks ")]
        public async Task CaseSensitiveToolNamesDenied(string tool)
        {
            (await _policy.IsAllowedAsync("u", tool, Scopes(ToolScope.Read), default))
                .Should().BeFalse();
        }

        [Theory]
        [InlineData("'; DROP TABLE Tasks; --")]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("../../etc/passwd")]
        public async Task InjectionAttemptsDenied(string tool)
        {
            (await _policy.IsAllowedAsync("u", tool, Scopes(ToolScope.Read, ToolScope.Write, ToolScope.Admin), default))
                .Should().BeFalse();
        }

        [Fact]
        public async Task ReadAndWriteScopesCanCallWrite()
        {
            var scopes = Scopes(ToolScope.Read, ToolScope.Write);
            (await _policy.IsAllowedAsync("u", "create_task", scopes, default)).Should().BeTrue();
        }

        [Fact]
        public async Task ReadAndWriteScopesCannotCallAdmin()
        {
            var scopes = Scopes(ToolScope.Read, ToolScope.Write);
            (await _policy.IsAllowedAsync("u", "delete_task", scopes, default)).Should().BeFalse();
        }

        [Fact]
        public async Task DuplicateScopesAreHandled()
        {
            var scopes = Scopes(ToolScope.Read, ToolScope.Read, ToolScope.Read);
            (await _policy.IsAllowedAsync("u", "list_tasks", scopes, default)).Should().BeTrue();
        } 

        [Fact]
        public async Task AdminOnlyCanCallAdminTool()
        {
            (await _policy.IsAllowedAsync("u", "delete_task", Scopes(ToolScope.Admin), default))
                .Should().BeTrue();
        }
    }
}

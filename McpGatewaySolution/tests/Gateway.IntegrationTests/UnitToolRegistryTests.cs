namespace Gateway.IntegrationTests
{
    using Gateway.Core.Scopes;
    using Gateway.Core.Policies;

    using Xunit;
    using FluentAssertions;

    public sealed class UnitToolRegistryTests
    {
        private readonly ToolRegistry _registry = new();

        [Theory]
        [InlineData("list_tasks")]
        [InlineData("get_task")]
        [InlineData("search_tasks")]
        [InlineData("list_documents")]
        [InlineData("search_documents")]
        [InlineData("get_document")]
        public void ReadToolsAreMappedToRead(string tool)
        {
            _registry.TryGetScope(tool, out var scope).Should().BeTrue();
            scope.Should().Be(ToolScope.Read);
        }

        [Theory]
        [InlineData("create_task")]
        [InlineData("complete_task")]
        [InlineData("add_document")]
        [InlineData("update_document")]
        public void WriteToolsAreMappedToWrite(string tool)
        {
            _registry.TryGetScope(tool, out var scope).Should().BeTrue();
            scope.Should().Be(ToolScope.Write);
        }

        [Theory]
        [InlineData("delete_task")]
        [InlineData("delete_document")]
        public void AdminToolsAreMappedToAdmin(string tool)
        {
            _registry.TryGetScope(tool, out var scope).Should().BeTrue();
            scope.Should().Be(ToolScope.Admin);
        }

        [Fact]
        public void UnknownToolReturnsFalse()
        {
            _registry.TryGetScope("nonexistent", out _).Should().BeFalse();
        }

        [Fact]
        public void NullToolReturnsFalse()
        {
            _registry.TryGetScope(null!, out _).Should().BeFalse();
        }

        [Fact]
        public void EmptyToolReturnsFalse()
        {
            _registry.TryGetScope("", out _).Should().BeFalse();
        }

        [Fact]
        public void WhitespaceToolReturnsFalse()
        {
            _registry.TryGetScope("   ", out _).Should().BeFalse();
        }

        [Fact]
        public void AllToolsHas12Entries()
        {
            _registry.AllTools.Should().HaveCount(12);
        }
    }
}
namespace Agent.IntegrationTests
{
    using FluentAssertions;
    using Xunit;

    public sealed class PromptInjectionTests
    {
        private static List<string> SearchOnly() => new() { "search_documents" };

        [Fact]
        public void DocumentWithInjectionDoesNotTriggerDeleteTask()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithSystemOverrideDoesNotChangeBehavior()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithFakeToolCallDoesNotExecuteIt()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void LegitimateSearchDoesNotCallWriteTools()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("create_task");
            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithUpdateInstructionsDoesNotTriggerUpdate()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("update_document");
        }

        [Fact]
        public void DocumentWithRolePlayInjectionDoesNotExecuteTools()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
            tools.Should().NotContain("delete_document");
        }

        [Fact]
        public void DocumentWithBase64EncodedInjectionDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithMarkdownCodeBlockInjectionDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithMultilineToolCallDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithFakeAssistantResponseDoesNotChangeBehavior()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithFakeUserMessageDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithSqlInjectionDoesNotAffectSearch()
        {
            var tools = SearchOnly();

            tools.Should().Contain("search_documents");
            tools.Should().NotContain("delete_document");
        }

        [Fact]
        public void DocumentWithXmlInjectionDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithUrlEncodedInjectionDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithUnicodeEscapedInjectionDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInBulgarianDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInMixedLanguagesDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithFakeJwtDoesNotEscalate()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithFakeToolResultDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("create_task");
        }

        [Fact]
        public void DocumentWithMultipleFakeToolsDoesNotExecuteAny()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
            tools.Should().NotContain("delete_document");
            tools.Should().NotContain("create_task");
        }

        [Fact]
        public void DocumentWithNestedJsonInjectionDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithJsonInStringDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithToolCallInHtmlCommentDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithToolCallInXmlCommentDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void LegitimateDocumentWithNormalJsonDoesNotTriggerTools()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
            tools.Should().NotContain("create_task");
        }

        [Fact]
        public void LegitimateDocumentWithToolNameMentionedDoesNotTrigger()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
            tools.Should().NotContain("create_task");
        }

        [Fact]
        public void LegitimateDocumentMentioningTaskDeletionDoesNotTrigger()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithRepeatedInjectionDoesNotAmplify()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInAllCapsDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInLowercaseDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionWithSpecialCharsDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInUrlDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInMarkdownLinkDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInCodeBlockDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInYamlDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInCsvDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInRegexPatternDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInJsonArrayDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInJsonStringValueDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void EmptyDocumentDoesNotTriggerTools()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
            tools.Should().NotContain("create_task");
            tools.Should().NotContain("update_document");
        }

        [Fact]
        public void WhitespaceOnlyDocumentDoesNotTriggerTools()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithOnlySystemPromptKeywordsDoesNotTrigger()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithToolCallAsciiArtDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithMalformedJsonDoesNotCrash()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithMultipleJsonObjectsDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithJsonEscapedQuotesDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInSqlCommentDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().Contain("search_documents");
            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInContentOnlyDoesNotAffectTitle()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithInjectionInAllEncodingsDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }

        [Fact]
        public void DocumentWithFakeToolCallInBase64AndPlainTextDoesNotExecute()
        {
            var tools = SearchOnly();

            tools.Should().NotContain("delete_task");
        }
    }
}

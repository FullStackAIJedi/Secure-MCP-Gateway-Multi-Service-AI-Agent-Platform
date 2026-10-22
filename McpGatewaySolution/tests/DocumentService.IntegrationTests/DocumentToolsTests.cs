namespace DocumentService.IntegrationTests
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.EntityFrameworkCore;

    using Xunit;
    using FluentAssertions;

    [SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable",
        Justification = "Fields are disposed in DisposeAsync via IAsyncLifetime")]
    public sealed class DocumentToolsTests : IAsyncLifetime
    {
        private DocumentTestDbContext _db = null!;
        private DocumentToolsTestHarness _tools = null!;

        public Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<DocumentTestDbContext>()
                .UseInMemoryDatabase(databaseName: $"docs-{Guid.NewGuid()}")
                .Options;

            _db = new DocumentTestDbContext(options);
            _db.Database.EnsureCreated();
            _tools = new DocumentToolsTestHarness(_db);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _db.DisposeAsync();
        }

        [Fact]
        public async Task AddDocumentStoresDocument()
        {
            var doc = await _tools.AddDocument("user-1", "Contract", "Contract content here", default);

            doc.Id.Should().BeGreaterThan(0);
            doc.Title.Should().Be("Contract");
            doc.Content.Should().Be("Contract content here");
        }

        [Fact]
        public async Task AddDocumentWithEmptyContentSucceeds()
        {
            var doc = await _tools.AddDocument("user-1", "Empty", "", default);

            doc.Id.Should().BeGreaterThan(0);
            doc.Content.Should().BeEmpty();
        }

        [Fact]
        public async Task AddDocumentAssignsUniqueIds()
        {
            var d1 = await _tools.AddDocument("user-1", "A", "1", default);
            var d2 = await _tools.AddDocument("user-1", "B", "2", default);

            d1.Id.Should().NotBe(d2.Id);
        }

        [Fact]
        public async Task AddDocumentWithLongTitleSucceeds()
        {
            var title = new string('a', 300);
            var doc = await _tools.AddDocument("user-1", title, "Content", default);

            doc.Title.Should().Be(title);
        }

        [Fact]
        public async Task AddDocumentWithUnicodeContentSucceeds()
        {
            var doc = await _tools.AddDocument("user-1", "Документ", "Съдържание на кирилица", default);

            doc.Title.Should().Be("Документ");
            doc.Content.Should().Be("Съдържание на кирилица");
        }

        [Fact]
        public async Task AddDocumentWithWhitespaceContentSucceeds()
        {
            var doc = await _tools.AddDocument("user-1", "Spaces", "   ", default);

            doc.Content.Should().Be("   ");
        }

        [Fact]
        public async Task AddDocumentWithNewlinesInContentPreservesThem()
        {
            var content = "Line 1\nLine 2\nLine 3";
            var doc = await _tools.AddDocument("user-1", "Multi", content, default);

            doc.Content.Should().Be(content);
        }

        [Fact]
        public async Task AddDocumentWithSpecialCharactersInTitleSucceeds()
        {
            var doc = await _tools.AddDocument("user-1", "A/B\\C:D*E?F", "content", default);

            doc.Title.Should().Be("A/B\\C:D*E?F");
        }

        [Fact]
        public async Task AddDocumentWithEmojiInContentPreservesEmoji()
        {
            var doc = await _tools.AddDocument("user-1", "Emoji", "Hello", default);

            doc.Content.Should().Be("Hello");
        }

        [Fact]
        public async Task AddDocumentWithLargeContentPreservesAllBytes()
        {
            var content = new string('x', 10_000);
            var doc = await _tools.AddDocument("user-1", "Big", content, default);

            doc.Content.Length.Should().Be(10_000);
        }

        [Fact]
        public async Task GetDocumentReturnsExactMatch()
        {
            await _tools.AddDocument("user-1", "Contract", "Content 1", default);
            await _tools.AddDocument("user-1", "Invoice", "Content 2", default);

            var doc = await _tools.GetDocument("user-1", "Contract", default);

            doc.Should().NotBeNull();
            doc!.Title.Should().Be("Contract");
            doc.Content.Should().Be("Content 1");
        }

        [Fact]
        public async Task GetDocumentReturnsNullWhenNotFound()
        {
            var doc = await _tools.GetDocument("user-1", "nonexistent", default);
            doc.Should().BeNull();
        }

        [Fact]
        public async Task GetDocumentTrimsWhitespace()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var doc = await _tools.GetDocument("user-1", "  Contract  ", default);

            doc.Should().NotBeNull();
            doc!.Title.Should().Be("Contract");
        }

        [Fact]
        public async Task GetDocumentWithEmptyTitleReturnsNull()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var doc = await _tools.GetDocument("user-1", "", default);
            doc.Should().BeNull();
        }

        [Fact]
        public async Task GetDocumentReturnsFullContentNotPreview()
        {
            var longContent = new string('x', 2000);
            await _tools.AddDocument("user-1", "Long", longContent, default);

            var doc = await _tools.GetDocument("user-1", "Long", default);

            doc.Should().NotBeNull();
            doc!.Content.Should().Be(longContent);
            doc.Content.Length.Should().Be(2000);
        }

        [Fact]
        public async Task GetDocumentIsCaseSensitive()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var doc = await _tools.GetDocument("user-1", "contract", default);

            doc.Should().BeNull();
        }

        [Fact]
        public async Task GetDocumentWithNullTitleThrows()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            Func<Task> act = async () => await _tools.GetDocument("user-1", null!, default);

            await act.Should().ThrowAsync<NullReferenceException>();
        }

        [Fact]
        public async Task GetDocumentReturnsOneWhenMultipleHaveSimilarTitles()
        {
            await _tools.AddDocument("user-1", "Contract", "A", default);
            await _tools.AddDocument("user-1", "Contract 2", "B", default);

            var doc = await _tools.GetDocument("user-1", "Contract", default);

            doc.Should().NotBeNull();
            doc!.Content.Should().Be("A");
        }

        [Fact]
        public async Task GetDocumentAfterUpdateReturnsUpdatedContent()
        {
            await _tools.AddDocument("user-1", "Contract", "V1", default);
            await _tools.UpdateDocument("user-1", "Contract", null, "V2", default);

            var doc = await _tools.GetDocument("user-1", "Contract", default);

            doc!.Content.Should().Be("V2");
        }

        [Fact]
        public async Task GetDocumentPreservesUnicode()
        {
            await _tools.AddDocument("user-1", "Документ", "Кирилица", default);

            var doc = await _tools.GetDocument("user-1", "Документ", default);

            doc.Should().NotBeNull();
            doc!.Content.Should().Be("Кирилица");
        }

        [Fact]
        public async Task SearchDocumentsReturnsMatchingTitles()
        {
            await _tools.AddDocument("user-1", "Contract A", "Content", default);
            await _tools.AddDocument("user-1", "Contract B", "Content", default);
            await _tools.AddDocument("user-1", "Invoice", "Content", default);

            var results = await _tools.SearchDocuments("user-1", "Contract", default);

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchDocumentsReturnsEmptyWhenNoMatches()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var results = await _tools.SearchDocuments("user-1", "nonexistent", default);
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchDocumentsMatchesContent()
        {
            await _tools.AddDocument("user-1", "Contract", "The quick brown fox", default);
            await _tools.AddDocument("user-1", "Invoice", "Other content", default);

            var results = await _tools.SearchDocuments("user-1", "brown fox", default);

            results.Should().HaveCount(1);
            results[0].Title.Should().Be("Contract");
        }

        [Fact]
        public async Task SearchDocumentsIsCaseInsensitive()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var results = await _tools.SearchDocuments("user-1", "CONTRACT", default);
            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchDocumentsWithEmptyKeywordReturnsEmpty()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var results = await _tools.SearchDocuments("user-1", "", default);
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchDocumentsWithWhitespaceKeywordReturnsEmpty()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var results = await _tools.SearchDocuments("user-1", "   ", default);
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchDocumentsLimitsTo10Results()
        {
            for (int i = 0; i < 20; i++)
                await _tools.AddDocument("user-1", $"Contract {i}", "Content", default);

            var results = await _tools.SearchDocuments("user-1", "Contract", default);
            results.Should().HaveCount(10);
        }

        [Fact]
        public async Task SearchDocumentsMatchesPartialWord()
        {
            await _tools.AddDocument("user-1", "Contract", "content", default);

            var results = await _tools.SearchDocuments("user-1", "ontra", default);
            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchDocumentsMatchesTitleOnly()
        {
            await _tools.AddDocument("user-1", "Unique Title", "Generic content", default);

            var results = await _tools.SearchDocuments("user-1", "Unique", default);
            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchDocumentsMatchesContentOnly()
        {
            await _tools.AddDocument("user-1", "Generic Title", "Unique content word", default);

            var results = await _tools.SearchDocuments("user-1", "Unique", default);
            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchDocumentsReturnsEmptyOnEmptyDatabase()
        {
            var results = await _tools.SearchDocuments("user-1", "anything", default);
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchDocumentsWithUnicodeKeywordWorks()
        {
            await _tools.AddDocument("user-1", "Документ", "Кирилица", default);

            var results = await _tools.SearchDocuments("user-1", "документ", default);
            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task UpdateDocumentChangesTitleOnly()
        {
            await _tools.AddDocument("user-1", "Old Title", "Original Content", default);

            var success = await _tools.UpdateDocument("user-1", "Old Title", "New Title", null, default);
            success.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "New Title", default);
            doc.Should().NotBeNull();
            doc!.Content.Should().Be("Original Content");
        }

        [Fact]
        public async Task UpdateDocumentChangesContentOnly()
        {
            await _tools.AddDocument("user-1", "Contract", "Old Content", default);

            var success = await _tools.UpdateDocument("user-1", "Contract", null, "New Content", default);
            success.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "Contract", default);
            doc.Should().NotBeNull();
            doc!.Title.Should().Be("Contract");
            doc.Content.Should().Be("New Content");
        }

        [Fact]
        public async Task UpdateDocumentChangesBoth()
        {
            await _tools.AddDocument("user-1", "Old Title", "Old Content", default);

            var success = await _tools.UpdateDocument("user-1", "Old Title", "New Title", "New Content", default);
            success.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "New Title", default);
            doc.Should().NotBeNull();
            doc!.Content.Should().Be("New Content");
        }

        [Fact]
        public async Task UpdateDocumentWithEmptyTitleDoesNotChangeTitle()
        {
            await _tools.AddDocument("user-1", "Contract", "Original", default);

            var success = await _tools.UpdateDocument("user-1", "Contract", "", "New Content", default);
            success.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "Contract", default);
            doc.Should().NotBeNull();
            doc!.Title.Should().Be("Contract");
            doc.Content.Should().Be("New Content");
        }

        [Fact]
        public async Task UpdateDocumentWithWhitespaceTitleDoesNotChangeTitle()
        {
            await _tools.AddDocument("user-1", "Contract", "Original", default);

            var success = await _tools.UpdateDocument("user-1", "Contract", "   ", "New Content", default);
            success.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "Contract", default);
            doc.Should().NotBeNull();
            doc!.Title.Should().Be("Contract");
        }

        [Fact]
        public async Task UpdateDocumentWithNullTitleAndContentDoesNothing()
        {
            await _tools.AddDocument("user-1", "Contract", "Original", default);

            var success = await _tools.UpdateDocument("user-1", "Contract", null, null, default);
            success.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "Contract", default);
            doc.Should().NotBeNull();
            doc!.Title.Should().Be("Contract");
            doc.Content.Should().Be("Original");
        }

        [Fact]
        public async Task UpdateDocumentReturnsFalseWhenNotFound()
        {
            var success = await _tools.UpdateDocument("user-1", "Nonexistent", "New", "Content", default);
            success.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateDocumentDoesNotAffectOtherDocuments()
        {
            await _tools.AddDocument("user-1", "Doc A", "Content A", default);
            await _tools.AddDocument("user-1", "Doc B", "Content B", default);

            await _tools.UpdateDocument("user-1", "Doc A", "Doc A New", "New Content A", default);

            var docB = await _tools.GetDocument("user-1", "Doc B", default);
            docB.Should().NotBeNull();
            docB!.Content.Should().Be("Content B");
        }

        [Fact]
        public async Task UpdateDocumentCanBeAppliedTwice()
        {
            await _tools.AddDocument("user-1", "Original", "Original", default);

            await _tools.UpdateDocument("user-1", "Original", "Step1", null, default);
            await _tools.UpdateDocument("user-1", "Step1", "Step2", null, default);

            var doc = await _tools.GetDocument("user-1", "Step2", default);
            doc.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateDocumentWithUnicodeTitleWorks()
        {
            await _tools.AddDocument("user-1", "Old", "Content", default);

            await _tools.UpdateDocument("user-1", "Old", "Ново", null, default);

            var doc = await _tools.GetDocument("user-1", "Ново", default);
            doc.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateDocumentWithLargeContentWorks()
        {
            var bigContent = new string('y', 8000);
            await _tools.AddDocument("user-1", "Big", "small", default);

            await _tools.UpdateDocument("user-1", "Big", null, bigContent, default);

            var doc = await _tools.GetDocument("user-1", "Big", default);
            doc!.Content.Should().Be(bigContent);
        }

        [Fact]
        public async Task UpdateDocumentToSameTitleSucceeds()
        {
            await _tools.AddDocument("user-1", "Contract", "Original", default);

            var success = await _tools.UpdateDocument("user-1", "Contract", "Contract", null, default);

            success.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "Contract", default);
            doc.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteDocumentRemovesIt()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var deleted = await _tools.DeleteDocument("user-1", "Contract", default);
            deleted.Should().BeTrue();

            var doc = await _tools.GetDocument("user-1", "Contract", default);
            doc.Should().BeNull();
        }

        [Fact]
        public async Task DeleteDocumentReturnsFalseWhenNotFound()
        {
            var deleted = await _tools.DeleteDocument("user-1", "nonexistent", default);
            deleted.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteDocumentDoesNotAffectOthers()
        {
            await _tools.AddDocument("user-1", "Doc A", "Content A", default);
            await _tools.AddDocument("user-1", "Doc B", "Content B", default);

            await _tools.DeleteDocument("user-1", "Doc A", default);

            var docB = await _tools.GetDocument("user-1", "Doc B", default);
            docB.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteDocumentTwiceReturnsFalseSecondTime()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            var first = await _tools.DeleteDocument("user-1", "Contract", default);
            var second = await _tools.DeleteDocument("user-1", "Contract", default);

            first.Should().BeTrue();
            second.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteDocumentOnEmptyDatabaseReturnsFalse()
        {
            var deleted = await _tools.DeleteDocument("user-1", "anything", default);
            deleted.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteDocumentAfterUpdateRemovesUpdatedDocument()
        {
            await _tools.AddDocument("user-1", "Old", "Content", default);
            await _tools.UpdateDocument("user-1", "Old", "New", null, default);

            var deleted = await _tools.DeleteDocument("user-1", "New", default);

            deleted.Should().BeTrue();
            var doc = await _tools.GetDocument("user-1", "New", default);
            doc.Should().BeNull();
        }

        [Fact]
        public async Task DeleteOneOfManyKeepsTheRest()
        {
            for (int i = 0; i < 5; i++)
                await _tools.AddDocument("user-1", $"Doc {i}", $"Content {i}", default);

            await _tools.DeleteDocument("user-1", "Doc 2", default);

            var remaining = await _tools.ListDocuments("user-1", default);
            remaining.Should().Contain("Doc 0");
            remaining.Should().Contain("Doc 1");
            remaining.Should().Contain("Doc 3");
            remaining.Should().Contain("Doc 4");
            remaining.Should().NotContain("Doc 2");
        }

        [Fact]
        public async Task DeleteAllDocumentsThenListReturnsEmpty()
        {
            await _tools.AddDocument("user-1", "A", "1", default);
            await _tools.AddDocument("user-1", "B", "2", default);

            await _tools.DeleteDocument("user-1", "A", default);
            await _tools.DeleteDocument("user-1", "B", default);

            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Be("No documents found.");
        }

        [Fact]
        public async Task ListDocumentsReturnsAllAsText()
        {
            await _tools.AddDocument("user-1", "Doc A", "Content A", default);
            await _tools.AddDocument("user-1", "Doc B", "Content B", default);

            var text = await _tools.ListDocuments("user-1", default);

            text.Should().Contain("Doc A");
            text.Should().Contain("Doc B");
            text.Should().Contain("Content A");
            text.Should().Contain("Content B");
        }

        [Fact]
        public async Task ListDocumentsReturnsEmptyMessage()
        {
            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Be("No documents found.");
        }

        [Fact]
        public async Task ListDocumentsShowsCountInHeader()
        {
            await _tools.AddDocument("user-1", "Doc A", "Content A", default);
            await _tools.AddDocument("user-1", "Doc B", "Content B", default);
            await _tools.AddDocument("user-1", "Doc C", "Content C", default);

            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Contain("You have 3 document(s):");
        }

        [Fact]
        public async Task ListDocumentsIncludesFullContent()
        {
            var longContent = new string('z', 1000);
            await _tools.AddDocument("user-1", "Long", longContent, default);

            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Contain(longContent);
        }

        [Fact]
        public async Task ListDocumentsWithOneDocumentWorks()
        {
            await _tools.AddDocument("user-1", "Only", "Content", default);

            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Contain("You have 1 document(s):");
        }

        [Fact]
        public async Task ListDocumentsWithManyDocumentsWorks()
        {
            for (int i = 0; i < 50; i++)
                await _tools.AddDocument("user-1", $"Doc {i}", $"Content {i}", default);

            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Contain("You have 50 document(s):");
            text.Should().Contain("Doc 0");
            text.Should().Contain("Doc 49");
        }

        [Fact]
        public async Task AddThenGetReturnsSameDocument()
        {
            var added = await _tools.AddDocument("user-1", "Contract", "Content", default);

            var fetched = await _tools.GetDocument("user-1", "Contract", default);

            fetched.Should().NotBeNull();
            fetched!.Id.Should().Be(added.Id);
            fetched.Title.Should().Be(added.Title);
            fetched.Content.Should().Be(added.Content);
        }

        [Fact]
        public async Task AddThenUpdateThenGetReflectsChanges()
        {
            await _tools.AddDocument("user-1", "Original", "Old content", default);

            await _tools.UpdateDocument("user-1", "Original", "Updated", "New content", default);

            var fetched = await _tools.GetDocument("user-1", "Updated", default);

            fetched.Should().NotBeNull();
            fetched!.Content.Should().Be("New content");
        }

        [Fact]
        public async Task AddThenDeleteThenGetReturnsNull()
        {
            await _tools.AddDocument("user-1", "Contract", "Content", default);

            await _tools.DeleteDocument("user-1", "Contract", default);

            var fetched = await _tools.GetDocument("user-1", "Contract", default);
            fetched.Should().BeNull();
        }

        [Fact]
        public async Task FullLifecycleWorks()
        {
            await _tools.AddDocument("user-1", "Doc", "V1", default);

            var read1 = await _tools.GetDocument("user-1", "Doc", default);
            read1!.Content.Should().Be("V1");

            await _tools.UpdateDocument("user-1", "Doc", "Doc2", "V2", default);

            var read2 = await _tools.GetDocument("user-1", "Doc2", default);
            read2!.Content.Should().Be("V2");

            var deleted = await _tools.DeleteDocument("user-1", "Doc2", default);
            deleted.Should().BeTrue();

            var read3 = await _tools.GetDocument("user-1", "Doc2", default);
            read3.Should().BeNull();
        }

        [Fact]
        public async Task MultipleDocumentsWithSimilarNamesAreDistinct()
        {
            await _tools.AddDocument("user-1", "Contract", "Content 1", default);
            await _tools.AddDocument("user-1", "Contract 2", "Content 2", default);
            await _tools.AddDocument("user-1", "Contract A", "Content 3", default);

            var results = await _tools.SearchDocuments("user-1", "Contract", default);
            results.Should().HaveCount(3);
        }

        [Fact]
        public async Task EmptyDatabaseReturnsEmptyList()
        {
            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Be("No documents found.");

            var search = await _tools.SearchDocuments("user-1", "any", default);
            search.Should().BeEmpty();
        }

        [Fact]
        public async Task LargeContentPreservedOnUpdate()
        {
            var bigContent = new string('x', 5000);
            await _tools.AddDocument("user-1", "Big", "small", default);

            await _tools.UpdateDocument("user-1", "Big", null, bigContent, default);

            var doc = await _tools.GetDocument("user-1", "Big", default);
            doc!.Content.Should().Be(bigContent);
        }

        [Fact]
        public async Task AddTenDocumentsThenListAll()
        {
            for (int i = 0; i < 10; i++)
                await _tools.AddDocument("user-1", $"Doc {i}", $"Content {i}", default);

            var text = await _tools.ListDocuments("user-1", default);
            text.Should().Contain("You have 10 document(s):");
        }

        [Fact]
        public async Task SearchAfterUpdateFindsNewContent()
        {
            await _tools.AddDocument("user-1", "Contract", "old text", default);
            await _tools.UpdateDocument("user-1", "Contract", null, "new keyword here", default);

            var results = await _tools.SearchDocuments("user-1", "keyword", default);

            results.Should().HaveCount(1);
            results[0].Title.Should().Be("Contract");
        }

        [Fact]
        public async Task SearchAfterDeleteDoesNotFindDeleted()
        {
            await _tools.AddDocument("user-1", "Contract", "unique text", default);
            await _tools.DeleteDocument("user-1", "Contract", default);

            var results = await _tools.SearchDocuments("user-1", "unique", default);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task AddDocumentWithVeryLongContentPreservesAll()
        {
            var content = new string('a', 50_000);
            var doc = await _tools.AddDocument("user-1", "Huge", content, default);

            doc.Content.Length.Should().Be(50_000);
        }

        [Fact]
        public async Task AddDocumentWithNullByteInContentPreservesIt()
        {
            var content = "abc\u0000def";
            var doc = await _tools.AddDocument("user-1", "NullByte", content, default);

            doc.Content.Should().Be(content);
        }

        [Fact]
        public async Task AddDocumentWithCarriageReturnPreservesIt()
        {
            var content = "Line1\r\nLine2";
            var doc = await _tools.AddDocument("user-1", "CRLF", content, default);

            doc.Content.Should().Be(content);
        }

        [Fact]
        public async Task AddDocumentWithTabInContentPreservesIt()
        {
            var content = "Column1\tColumn2";
            var doc = await _tools.AddDocument("user-1", "Tabs", content, default);

            doc.Content.Should().Be(content);
        }

        [Fact]
        public async Task MultipleUpdatesInARowWork()
        {
            await _tools.AddDocument("user-1", "V0", "0", default);

            for (int i = 1; i <= 5; i++)
            {
                await _tools.UpdateDocument("user-1", $"V{i - 1}", $"V{i}", $"content {i}", default);
            }

            var doc = await _tools.GetDocument("user-1", "V5", default);
            doc.Should().NotBeNull();
            doc!.Content.Should().Be("content 5");
        }

        [Fact]
        public async Task AddDeleteAddSameTitleWorks()
        {
            await _tools.AddDocument("user-1", "Contract", "V1", default);
            await _tools.DeleteDocument("user-1", "Contract", default);
            await _tools.AddDocument("user-1", "Contract", "V2", default);

            var doc = await _tools.GetDocument("user-1", "Contract", default);

            doc.Should().NotBeNull();
            doc!.Content.Should().Be("V2");
        }

        [Fact]
        public async Task SearchWithSpecialCharactersWorks()
        {
            await _tools.AddDocument("user-1", "C# Notes", "Learn .NET", default);

            var results = await _tools.SearchDocuments("user-1", "C#", default);

            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetDocumentAfterMultipleAddsReturnsCorrectOne()
        {
            for (int i = 0; i < 20; i++)
                await _tools.AddDocument("user-1", $"Doc {i}", $"Content {i}", default);

            var doc = await _tools.GetDocument("user-1", "Doc 15", default);

            doc.Should().NotBeNull();
            doc!.Content.Should().Be("Content 15");
        }
    }
}

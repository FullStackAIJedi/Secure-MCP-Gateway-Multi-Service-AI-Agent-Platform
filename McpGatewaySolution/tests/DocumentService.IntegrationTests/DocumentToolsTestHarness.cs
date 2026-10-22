namespace DocumentService.IntegrationTests
{
    using Microsoft.EntityFrameworkCore;

    public sealed class DocumentToolsTestHarness(DocumentTestDbContext db)
    {
        public async Task<string> ListDocuments(string subjectId, CancellationToken cancellationToken)
        {
            var docs = await db.Documents
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new { d.Id, d.Title, Content = d.Content ?? string.Empty })
                .ToListAsync(cancellationToken);

            if (docs.Count == 0)
                return "No documents found.";

            var lines = new List<string> { $"You have {docs.Count} document(s):" };
            foreach (var doc in docs)
            {
                lines.Add($"- Title: {doc.Title}");
                lines.Add($"  Content: {doc.Content}");
            }

            return string.Join("\n", lines);
        }

        public async Task<List<DocumentSummary>> SearchDocuments(
            string subjectId, string keyword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return [];

            var lower = keyword.ToLowerInvariant();

            var docs = await db.Documents
                .AsNoTracking()
                .Select(d => new DocumentSummary(d.Id, d.Title, d.Content))
                .ToListAsync(cancellationToken);

            return docs
                .Where(d => d.Title.ToLowerInvariant().Contains(lower, StringComparison.Ordinal) ||
                            d.Content.ToLowerInvariant().Contains(lower, StringComparison.Ordinal))
                .Take(10)
                .ToList();
        }

        public async Task<DocumentSummary?> GetDocument(
            string subjectId, string title, CancellationToken cancellationToken)
        {
            var trimmed = title.Trim();
            var doc = await db.Documents.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Title == trimmed, cancellationToken);

            if (doc is null) return null;

            return new DocumentSummary(doc.Id, doc.Title, doc.Content);
        }

        public async Task<DocumentSummary> AddDocument(
            string subjectId, string title, string content, CancellationToken cancellationToken)
        {
            var doc = new TestDocument { Title = title, Content = content };
            db.Documents.Add(doc);
            await db.SaveChangesAsync(cancellationToken);
            return new DocumentSummary(doc.Id, doc.Title, doc.Content);
        }

        public async Task<bool> UpdateDocument(
            string subjectId, string currentTitle, string? newTitle, string? content,
            CancellationToken cancellationToken)
        {
            var doc = await db.Documents
                .FirstOrDefaultAsync(d => d.Title == currentTitle, cancellationToken);

            if (doc is null) return false;

            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                doc.Title = newTitle;
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                doc.Content = content;
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteDocument(
            string subjectId, string title, CancellationToken cancellationToken)
        {
            var doc = await db.Documents
                .FirstOrDefaultAsync(d => d.Title == title, cancellationToken);

            if (doc is null) return false;

            db.Documents.Remove(doc);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public sealed record DocumentSummary(int Id, string Title, string Content);
}

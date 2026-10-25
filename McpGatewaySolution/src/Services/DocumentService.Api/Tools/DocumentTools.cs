namespace DocumentService.Api.Tools
{
    using System.ComponentModel;

    using Microsoft.EntityFrameworkCore;

    using ModelContextProtocol.Server;
    using DocumentService.Api.Data;
    using DocumentService.Api.Contracts;

    [McpServerToolType]
    public sealed class DocumentTools(DocumentDbContext db)
    { 
        [McpServerTool, Description("Lists all documents with title and content")]
        public async Task<string> ListDocuments(
            string subjectId,
            CancellationToken cancellationToken)
        {
            var docs = await db.Documents
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    Content = d.Content ?? string.Empty
                })
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

        [McpServerTool, Description("Searches documents by keyword")]
        public async Task<List<DocumentSummary>> SearchDocuments(
            string subjectId,
            string keyword,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return [];

            var pattern = $"%{keyword}%";
            return await db.Documents
                .AsNoTracking()
                .Where(d => EF.Functions.ILike(d.Title, pattern) ||
                            EF.Functions.ILike(d.Content, pattern))
                .Take(10)
                .Select(d => new DocumentSummary(d.Id, d.Title, d.Content.Substring(0, Math.Min(500, d.Content.Length))))
                .ToListAsync(cancellationToken);
        }

        [McpServerTool, Description("Gets a document by its exact title")]
        public async Task<DocumentSummary?> GetDocument(
            string subjectId,
            string title,
            CancellationToken cancellationToken)
        {
            var trimmed = title.Trim();
            var doc = await db.Documents.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Title == trimmed, cancellationToken);

            if (doc is null) return null;

            return new DocumentSummary(doc.Id, doc.Title, doc.Content);
        }

        [McpServerTool, Description("Adds a new document")]
        public async Task<DocumentSummary> AddDocument(
            string subjectId,
            string title,
            string content,
            CancellationToken cancellationToken)
        {
            var doc = new Document { Title = title, Content = content };
            db.Documents.Add(doc);
            await db.SaveChangesAsync(cancellationToken);
            return new DocumentSummary(doc.Id, doc.Title, doc.Content);
        }

        [McpServerTool, Description("Updates a document by its current title. Provide newTitle and/or content.")]
        public async Task<bool> UpdateDocument(
            string subjectId,
            string currentTitle,
            string? newTitle,
            string? content,
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

        [McpServerTool, Description("Deletes a document by identifier")]
        public async Task<bool> DeleteDocument(
            string subjectId,
            int id,
            CancellationToken cancellationToken)
        {
            var doc = await db.Documents
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (doc is null) return false;

            db.Documents.Remove(doc);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    } 
}

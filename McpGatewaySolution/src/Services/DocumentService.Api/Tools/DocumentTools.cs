namespace DocumentService.Api.Tools
{
    using System.ComponentModel;
    using Microsoft.EntityFrameworkCore;

    using ModelContextProtocol.Server;
    using DocumentService.Api.Data;

    [McpServerToolType]
    public sealed class DocumentTools(DocumentDbContext db)
    {
        [McpServerTool, Description("Searches documents by keyword")]
        public async Task<List<DocumentSummary>> SearchDocuments(string keyword, CancellationToken cancellationToken)
        {
            return await db.Documents
                .AsNoTracking()
                .Where(d => d.Title.Contains(keyword) || d.Content.Contains(keyword))
                .Take(10)
                .Select(d => new DocumentSummary(d.Id, d.Title, d.Content.Substring(0, Math.Min(500, d.Content.Length))))
                .ToListAsync(cancellationToken);
        }

        [McpServerTool, Description("Gets a document by identifier")]
        public async Task<Document?> GetDocument(int id, CancellationToken cancellationToken)
        {
            return await db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        [McpServerTool, Description("Adds a new document")]
        public async Task<DocumentSummary> AddDocument(string title, string content, CancellationToken cancellationToken)
        {
            var document = new Document { Title = title, Content = content };
            db.Documents.Add(document);
            await db.SaveChangesAsync(cancellationToken);
            return new DocumentSummary(document.Id, document.Title, document.Content);
        }

        [McpServerTool, Description("Gets a document by identifier")]
        public async Task<Document?> GetDocument(
            string subjectId,
            int id,
            CancellationToken cancellationToken)
        {
            return await db.Documents.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
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
    }

    public sealed record DocumentSummary(int Id, string Title, string ContentPreview);
}

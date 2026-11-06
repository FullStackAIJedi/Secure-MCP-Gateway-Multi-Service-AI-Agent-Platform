namespace DocumentService.Api.Tools
{
    using System.ComponentModel;

    using Microsoft.EntityFrameworkCore;

    using ModelContextProtocol.Server;

    using DocumentService.Api.Data;
    using DocumentService.Api.Contracts;
    using DocumentService.Api.Entities;

    [McpServerToolType]
    public sealed class DocumentTools(DocumentDbContext db)
    {
        [McpServerTool, Description("Lists all documents for the current user")]
        public async Task<List<DocumentSummary>> ListDocuments(
            string subjectId,
            CancellationToken cancellationToken)
        {
            return await db.Documents
                .AsNoTracking()
                .Where(d => d.OwnerSubjectId == subjectId)
                .OrderBy(d => d.Id)
                .Select(d => new DocumentSummary(d.Id, d.Title, d.Description))
                .ToListAsync(cancellationToken);
        }

        [McpServerTool, Description("Searches documents by keyword in title or description")]
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
                .Where(d => d.OwnerSubjectId == subjectId &&
                            (EF.Functions.ILike(d.Title, pattern) ||
                             EF.Functions.ILike(d.Description, pattern)))
                .OrderBy(d => d.Id)    
                .Take(10)
                .Select(d => new DocumentSummary(
                    d.Id,
                    d.Title,
                    d.Description.Substring(0, Math.Min(500, d.Description.Length))))
                .ToListAsync(cancellationToken);
        }

        [McpServerTool, Description("Gets a document by id (preferred) or exact title.")]
        public async Task<DocumentSummary?> GetDocument(
            string subjectId,
            [Description("Document id. Preferred lookup key.")]
            int? id = null,
            [Description("Exact document title. Alternative to id.")]
            string? title = null,
            CancellationToken cancellationToken = default)
        {
            if (id is null && string.IsNullOrWhiteSpace(title))
                return null;

            var doc = id is not null
                ? await db.Documents.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id && d.OwnerSubjectId == subjectId, cancellationToken)
                : await db.Documents.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Title == title!.Trim() && d.OwnerSubjectId == subjectId, cancellationToken);

            if (doc is null) return null;

            return new DocumentSummary(doc.Id, doc.Title, doc.Description);
        }

        [McpServerTool, Description(
            "Creates a new document for the current user. Title is required, description is optional.")]
        public async Task<DocumentSummary> CreateDocument(
            string subjectId,
            [Description("Document title. Required.")]
            string title,
            [Description("Document description. Optional — pass null or empty if not provided.")]
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new InvalidOperationException("Error: 'title' is required.");

            var doc = new Document
            {
                Title = title,
                Description = description ?? string.Empty,
                OwnerSubjectId = subjectId
            };
            db.Documents.Add(doc);
            await db.SaveChangesAsync(cancellationToken);

            return new DocumentSummary(doc.Id, doc.Title, doc.Description);
        }

        [McpServerTool, Description(
            "Updates an existing document identified by its EXACT current title. " +
            "You can change the title, the description, or both. " +
            "You MUST pass 'currentTitle' (the exact existing title). " +
            "Pass 'newTitle' only if you want to rename. " +
            "Pass 'description' only if you want to change the description.")]
        public async Task<string> UpdateDocument(
            string subjectId,
            [Description("The EXACT current title of the document to update. Required — lookup key.")]
            string currentTitle,
            [Description("Optional. The new title. Omit to keep current title.")]
            string? newTitle = null,
            [Description("Optional. The new description. Omit to keep current description.")]
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(currentTitle))
                return "Error: 'currentTitle' is required. Call list_documents first to see the exact titles.";

            var hasNewTitle = !string.IsNullOrWhiteSpace(newTitle);
            var hasNewDescription = description is not null;

            if (!hasNewTitle && !hasNewDescription)
                return "Error: nothing to update. Provide 'newTitle' and/or 'description'.";

            var doc = await db.Documents
                .FirstOrDefaultAsync(
                    d => d.OwnerSubjectId == subjectId && d.Title == currentTitle,
                    cancellationToken);

            if (doc is null)
            {
                var available = await db.Documents
                    .AsNoTracking()
                    .Where(d => d.OwnerSubjectId == subjectId)
                    .Select(d => d.Title)
                    .ToListAsync(cancellationToken);

                return available.Count == 0
                    ? $"Error: document with title '{currentTitle}' not found. You have no documents."
                    : $"Error: document with title '{currentTitle}' not found. " +
                      $"Available titles: {string.Join(", ", available.Select(t => $"'{t}'"))}";
            }

            var changes = new List<string>();

            if (hasNewTitle)
            {
                changes.Add($"title '{doc.Title}' -> '{newTitle}'");
                doc.Title = newTitle!;
            }

            if (hasNewDescription)
            {
                changes.Add("description updated");
                doc.Description = description!;
            }

            await db.SaveChangesAsync(cancellationToken);

            return $"Done!";
        }

        [McpServerTool, Description(
            "Deletes a document. Pass either 'id' (preferred) or 'title' (exact current title).")]
        public async Task<string> DeleteDocument(
            string subjectId,
            [Description("Document id. Preferred lookup key.")]
            int? id = null,
            [Description("Exact document title. Alternative to id.")]
            string? title = null,
            CancellationToken cancellationToken = default)
        {
            if (id is null && string.IsNullOrWhiteSpace(title))
                return "Error: provide 'id' or 'title'.";

            var doc = id is not null
                ? await db.Documents.FirstOrDefaultAsync(
                    d => d.Id == id && d.OwnerSubjectId == subjectId, cancellationToken)
                : await db.Documents.FirstOrDefaultAsync(
                    d => d.Title == title!.Trim() && d.OwnerSubjectId == subjectId, cancellationToken);

            if (doc is null) return "Error: document not found.";

            db.Documents.Remove(doc);
            await db.SaveChangesAsync(cancellationToken);
            return $"Document '{doc.Title}' deleted.";
        }
    }
}

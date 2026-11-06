namespace DocumentService.Api.Entities;

public sealed class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Pgvector.Vector? Embedding { get; set; }
    public string OwnerSubjectId { get; set; } = string.Empty; 
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

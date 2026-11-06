namespace Gateway.Api.Contracts;

public sealed record UpdateDocumentRequest(string? NewTitle, string? Content);

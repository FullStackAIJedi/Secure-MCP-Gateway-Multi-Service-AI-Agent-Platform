namespace DocumentService.Api.Contracts
{
    public sealed record DocumentSummary(
        int Id,
        string Title,
        string Content);
}
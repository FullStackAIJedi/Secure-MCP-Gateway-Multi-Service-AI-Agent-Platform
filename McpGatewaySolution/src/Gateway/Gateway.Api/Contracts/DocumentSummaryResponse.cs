namespace Gateway.Api.Contracts
{
    public sealed record DocumentSummaryResponse(
        int Id,
        string Title,
        string Content);
}

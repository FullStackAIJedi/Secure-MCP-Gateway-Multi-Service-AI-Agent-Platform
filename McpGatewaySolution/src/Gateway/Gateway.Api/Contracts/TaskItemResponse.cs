namespace Gateway.Api.Contracts
{
    public sealed record TaskItemResponse(
            int Id,
            string Title,
            string Description,
            bool IsComplete,
            DateTimeOffset CreatedAt);
}

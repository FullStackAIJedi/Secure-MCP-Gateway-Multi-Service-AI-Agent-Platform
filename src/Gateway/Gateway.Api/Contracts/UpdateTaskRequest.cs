namespace Gateway.Api.Contracts;

public sealed record UpdateTaskRequest(string? NewTitle, string? Description);

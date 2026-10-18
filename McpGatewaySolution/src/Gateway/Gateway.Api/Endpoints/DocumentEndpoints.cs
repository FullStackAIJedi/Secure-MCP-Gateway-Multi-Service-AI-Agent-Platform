namespace Gateway.Api.Endpoints
{ 
    using System.Security.Claims;
    using System.Text.Json;
 
    using Microsoft.AspNetCore.Mvc;

    using Gateway.Api.Tools;

    public static class DocumentEndpoints
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static void MapDocumentEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/documents")
                .WithTags("Documents")
                .RequireAuthorization("mcp.read");

            group.MapGet("/search", Search)
                .WithName("SearchDocuments");
        }

        private static async Task<IResult> Search(
            [FromQuery] string keyword,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (subjectId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(keyword))
                return Results.Ok(Array.Empty<DocumentSummaryResponse>());

            try
            {
                var json = await tools.SearchDocuments(subjectId, keyword, cancellationToken);
                var documents = JsonSerializer.Deserialize<List<DocumentSummaryResponse>>(json, JsonOptions);
                return Results.Ok(documents ?? []);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }
    }

    public sealed record DocumentSummaryResponse(
        int Id,
        string Title,
        string ContentPreview);
}
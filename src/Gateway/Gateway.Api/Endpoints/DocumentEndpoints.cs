namespace Gateway.Api.Endpoints
{
    using System.Security.Claims;
    using System.Text.Json;

    using Microsoft.AspNetCore.Mvc;

    using Gateway.Api.Tools;
    using Gateway.Api.Contracts;

    public static class DocumentEndpoints
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static void MapDocumentEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/v1/documents")
                .WithTags("Documents")
                .RequireAuthorization("mcp.read");

            group.MapGet("/", ListDocuments)
                .WithName("ListDocuments");

            group.MapGet("/search", SearchDocuments)
                .WithName("SearchDocuments");

            group.MapGet("/by-title/{title}", GetDocumentByTitle)
                .WithName("GetDocumentByTitle");

            group.MapPost("/", CreateDocument)
                .RequireAuthorization("mcp.write")
                .WithName("CreateDocument");

            group.MapPut("/by-title/{currentTitle}", UpdateDocumentByTitle)
                .RequireAuthorization("mcp.write")
                .WithName("UpdateDocumentByTitle");

            group.MapDelete("/{id:int}", DeleteDocument)
                .RequireAuthorization("mcp.admin")
                .WithName("DeleteDocument");
        }

        private static async Task<IResult> ListDocuments(
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            try
            {
                var json = await tools.ListDocuments(subjectId, cancellationToken);
                var documents = JsonSerializer.Deserialize<List<DocumentSummaryResponse>>(json, JsonOptions);
                return Results.Ok(documents ?? []);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.Problem($"ListDocuments failed: {ex.Message}");
            }
        }

        private static async Task<IResult> SearchDocuments(
            [FromQuery] string keyword,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
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

        private static async Task<IResult> GetDocumentByTitle(
            string title,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(title))
                return Results.BadRequest(new { error = "Title is required" });

            try
            {
                var json = await tools.GetDocument(subjectId, title, cancellationToken);
                if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
                    return Results.NotFound();

                var document = JsonSerializer.Deserialize<DocumentSummaryResponse>(json, JsonOptions);
                return document is null ? Results.NotFound() : Results.Ok(document);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static async Task<IResult> CreateDocument(
            CreateDocumentRequest request,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Title is required" });

            try
            {
                var message = await tools.CreateDocument(   
                    subjectId, request.Title, request.Content ?? string.Empty, cancellationToken);
                return Results.Created("/api/v1/documents", new { message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        //private static async Task<IResult> UpdateDocumentByTitle(
        //    string currentTitle,
        //    UpdateDocumentRequest request,
        //    [FromServices] GatewayTools tools,
        //    HttpContext context,
        //    CancellationToken cancellationToken)
        //{
        //    var subjectId = GetSubjectId(context);
        //    if (subjectId is null) return Results.Unauthorized();

        //    if (string.IsNullOrWhiteSpace(currentTitle))
        //        return Results.BadRequest(new { error = "Current title is required" });

        //    try
        //    {
        //        var json = await tools.UpdateDocument(
        //            subjectId, currentTitle, request.NewTitle, request.Content, cancellationToken);

        //        var success = JsonSerializer.Deserialize<bool>(json, JsonOptions);
        //        return success ? Results.NoContent() : Results.NotFound();
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        return Results.Forbid();
        //    }
        //}
        private static async Task<IResult> UpdateDocumentByTitle(
    string currentTitle,
    UpdateDocumentRequest request,
    [FromServices] GatewayTools tools,
    HttpContext context,
    CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(currentTitle))
                return Results.BadRequest(new { error = "Current title is required" });

            try
            {
                var message = await tools.UpdateDocument(
                    subjectId, currentTitle, request.NewTitle, request.Content, cancellationToken);

                if (string.IsNullOrWhiteSpace(message))
                    return Results.NotFound();

                if (message.StartsWith("Error:", StringComparison.Ordinal))
                    return Results.BadRequest(new { error = message });

                return Results.Ok(new { message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static async Task<IResult> DeleteDocument(
            int id,
            [FromServices] GatewayTools tools,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            var subjectId = GetSubjectId(context);
            if (subjectId is null) return Results.Unauthorized();

            try
            {
                var message = await tools.DeleteDocument(subjectId, id, cancellationToken);

                if (string.IsNullOrWhiteSpace(message))
                    return Results.NotFound();

                if (message.StartsWith("Error:", StringComparison.Ordinal))
                    return Results.NotFound();

                return Results.NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }

        private static string? GetSubjectId(HttpContext context)
        {
            return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}

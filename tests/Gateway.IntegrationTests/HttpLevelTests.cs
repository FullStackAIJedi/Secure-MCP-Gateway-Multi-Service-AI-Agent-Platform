namespace Gateway.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using FluentAssertions;

using Xunit;

using Gateway.Api.Contracts;

public sealed class HttpLevelTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HttpLevelTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetTasksWithoutAuthReturns401()
    {
        var response = await _client.GetAsync("/api/v1/tasks");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLogsWithReadOnlyTokenReturns403()
    {
        var token = TestJwtFactory.CreateToken(
            subjectId: Guid.NewGuid().ToString(),
            email: "readonly@test.com",
            scopes: ["mcp.tools.read"]);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/security/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTaskWithWriteTokenReturns403()
    {
        var token = TestJwtFactory.CreateToken(
            subjectId: Guid.NewGuid().ToString(),
            email: "writer@test.com",
            scopes: ["mcp.tools.read", "mcp.tools.write"]);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync("/api/v1/internal/delete-task?id=1", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    } 

    [Fact]
    public async Task McpEndpointExceedingRateLimitReturns429()
    {
        var token = TestJwtFactory.CreateToken(
            subjectId: Guid.NewGuid().ToString(),
            email: "ratelimit@test.com",
            scopes: ["mcp.tools.read", "mcp.tools.write", "mcp.tools.admin"]);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        for (int i = 0; i < 100; i++)
        {
            await _client.PostAsync("/mcp", null);
        }

        var response = await _client.PostAsync("/mcp", null);
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}

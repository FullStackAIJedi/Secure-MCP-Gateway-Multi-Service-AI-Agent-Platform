namespace Gateway.Api.Endpoints
{  
    public static class GatewayEndpoints
    {
        private static readonly string[] DownstreamServices =
        [
            "taskservice",
            "documentservice"
        ];

        public static void MapGatewayEndpoints(this WebApplication app)
        {
            app.MapGet("/api/v1/gateway/info", () => Results.Ok(new
            {
                service = "MCP Gateway",
                version = "1.0.0",
                timestamp = DateTimeOffset.UtcNow,
                downstream = DownstreamServices
            }))
            .AllowAnonymous()
            .WithTags("Gateway")
            .WithName("GatewayInfo");
        }
    }
}

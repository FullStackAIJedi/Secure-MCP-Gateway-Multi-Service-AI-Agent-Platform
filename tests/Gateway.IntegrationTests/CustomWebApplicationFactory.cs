namespace Gateway.IntegrationTests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Gateway.Api.Data;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeMcpServer McpServer { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<UserDbContext>>();
            services.RemoveAll<UserDbContext>();

            services.AddDbContext<UserDbContext>(options =>
            {
                options.UseInMemoryDatabase("gateway-tests");
            });

            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(_ => new FakeHttpClientFactory(McpServer));
        });
    }
}

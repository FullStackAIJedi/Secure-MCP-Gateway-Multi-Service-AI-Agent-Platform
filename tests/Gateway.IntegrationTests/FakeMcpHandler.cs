namespace Gateway.IntegrationTests;

using System.Net;
using System.Text;

public sealed class FakeMcpHandler : HttpMessageHandler
{
    private readonly FakeMcpServer _server;

    public FakeMcpHandler(FakeMcpServer server)
    {
        _server = server;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var responseBody = _server.Handle(body);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }
}

namespace Gateway.IntegrationTests;

public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly FakeMcpServer _server;

    public FakeHttpClientFactory(FakeMcpServer server)
    {
        _server = server;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new FakeMcpHandler(_server))
        {
            BaseAddress = new Uri("http://fake-downstream")
        };
    }
}

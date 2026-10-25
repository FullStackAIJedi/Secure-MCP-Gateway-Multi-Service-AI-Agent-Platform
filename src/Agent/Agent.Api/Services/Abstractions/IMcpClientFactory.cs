namespace Agent.Api.Services.Abstractions; 

public interface IMcpClientFactory
{
    Task<IMcpClient> CreateAsync(string jwt, CancellationToken cancellationToken);
}

namespace Agent.Api.Services.Abstractions;

using Microsoft.Extensions.AI; 

public interface IChatClientFactory
{
    IChatClient Create(string model);
}

namespace Agent.Api.Services.Implementations;

using Microsoft.Extensions.AI;

using OpenAI;
using Agent.Api.Services.Abstractions; 

public sealed class ChatClientFactory(IConfiguration configuration) : IChatClientFactory
{
    public IChatClient Create(string model)
    {
        var ollamaUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";

        var openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential("ollama"),
            new OpenAIClientOptions
            {
                Endpoint = new Uri($"{ollamaUrl.TrimEnd('/')}/v1")
            });

        return openAiClient.GetChatClient(model).AsIChatClient();
    }
}

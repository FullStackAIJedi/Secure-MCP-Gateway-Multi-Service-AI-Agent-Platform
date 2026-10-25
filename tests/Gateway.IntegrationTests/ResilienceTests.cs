namespace Gateway.IntegrationTests
{
    using System.Net;

    using Xunit;
    using FluentAssertions;

    using Polly;
    using Polly.Retry;

    public sealed class ResilienceTests
    {
        private sealed class CountingHandler : DelegatingHandler
        {
            public int Attempts { get; private set; }
            public HttpStatusCode[] Responses { get; }

            public CountingHandler(params HttpStatusCode[] responses)
            {
                Responses = responses;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var status = Attempts < Responses.Length
                    ? Responses[Attempts]
                    : HttpStatusCode.OK;
                Attempts++;
                return Task.FromResult(new HttpResponseMessage(status));
            }
        }

        [Fact]
        public async Task RetryHandlerRetriesOn500()
        {
            var handler = new CountingHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);

            var client = new HttpClient(handler);

            var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 3,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError)
                })
                .Build();

            var result = await pipeline.ExecuteAsync(async ct => await client.GetAsync("http://test", ct));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.Attempts.Should().Be(3);
        }

        [Fact]
        public async Task RetryHandlerGivesUpAfterMaxAttempts()
        {
            var handler = new CountingHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError);

            var client = new HttpClient(handler);

            var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 3,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError)
                })
                .Build();

            var result = await pipeline.ExecuteAsync(async ct => await client.GetAsync("http://test", ct));

            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            handler.Attempts.Should().Be(4); // 1 original + 3 retries
        }
    }
}

namespace Microsoft.Extensions.Hosting
{
    using System;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting.Observability;
    using Microsoft.Extensions.Logging;

    using OpenTelemetry;
    using OpenTelemetry.Exporter;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;

    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Compact;

    public static class Extensions
    {
        private const string HealthEndpointPath = "/health";
        private const string AlivenessEndpointPath = "/health/live";
        private const string ReadinessEndpointPath = "/health/ready";

        public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            Console.Error.WriteLine("[SD] 1. AddServiceDefaults START");

            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            Console.Error.WriteLine("[SD] 2. Logging filters OK");

            builder.ConfigureOpenTelemetry();
            Console.Error.WriteLine("[SD] 3. ConfigureOpenTelemetry OK");

            builder.AddDefaultHealthChecks();
            Console.Error.WriteLine("[SD] 4. Health checks OK");

            builder.Services.AddServiceDiscovery();
            Console.Error.WriteLine("[SD] 5. Service discovery OK");

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler(options =>
                {
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.UseJitter = true;
                    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                });
                http.AddServiceDiscovery();
            });
            Console.Error.WriteLine("[SD] 6. HTTP client defaults OK");

            builder.Services.AddSerilog((services, lc) => lc
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.With<TraceEnricher>()
                .Enrich.WithProperty("service.name", builder.Environment.ApplicationName)
                .WriteTo.Console(new CompactJsonFormatter())
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("OpenTelemetry", LogEventLevel.Debug));
            Console.Error.WriteLine("[SD] 7. Serilog OK");

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Extensions["traceId"] =
                        System.Diagnostics.Activity.Current?.TraceId.ToHexString();
                    context.ProblemDetails.Extensions["spanId"] =
                        System.Diagnostics.Activity.Current?.SpanId.ToHexString();
                };
            });
            Console.Error.WriteLine("[SD] 8. Problem details OK");

            Console.Error.WriteLine("[SD] 9. AddServiceDefaults END");
            return builder;
        }

        public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            Console.Error.WriteLine("[OTEL] 1. ConfigureOpenTelemetry START");

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
            Console.Error.WriteLine("[OTEL] 2. Logging OK");

            var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            Console.Error.WriteLine($"[OTEL] 3. Endpoint = {endpoint ?? "<null>"}");

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                Console.Error.WriteLine($"[OTEL] 3b. Endpoint from config = {endpoint ?? "<null>"}");
            }

            var otel = builder.Services.AddOpenTelemetry();
            Console.Error.WriteLine("[OTEL] 4. AddOpenTelemetry OK");

            otel.WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(TelemetryConstants.AgentMeterName)
                    .AddMeter(TelemetryConstants.GatewayMeterName)
                    .AddMeter(TelemetryConstants.TaskServiceMeterName)
                    .AddMeter(TelemetryConstants.DocumentServiceMeterName);

                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    Console.Error.WriteLine($"[OTEL] 5a. Adding OTLP exporter for metrics: {endpoint}");
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                    Console.Error.WriteLine("[OTEL] 5a. OTLP exporter for metrics OK");
                }
            });
            Console.Error.WriteLine("[OTEL] 5. WithMetrics OK");

            otel.WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource(TelemetryConstants.AgentSourceName)
                    .AddSource(TelemetryConstants.GatewaySourceName)
                    .AddSource(TelemetryConstants.TaskServiceSourceName)
                    .AddSource(TelemetryConstants.DocumentServiceSourceName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                            && !context.Request.Path.StartsWithSegments(ReadinessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    Console.Error.WriteLine($"[OTEL] 6a. Adding OTLP exporter for traces: {endpoint}");
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                    Console.Error.WriteLine("[OTEL] 6a. OTLP exporter for traces OK");
                }
                else
                {
                    Console.Error.WriteLine("[OTEL] 6a. Endpoint is null — SKIPPING traces exporter");
                }
            });
            Console.Error.WriteLine("[OTEL] 6. WithTracing OK");

            Console.Error.WriteLine("[OTEL] 7. ConfigureOpenTelemetry END");
            return builder;
        }

        public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
            where TBuilder : IHostApplicationBuilder
        {
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
            return builder;
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
            app.MapHealthChecks(ReadinessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => !r.Tags.Contains("live")
            });

            return app;
        }
    }
}

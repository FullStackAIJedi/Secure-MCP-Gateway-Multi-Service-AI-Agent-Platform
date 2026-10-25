namespace Microsoft.Extensions.Hosting.Observability
{
    using System.Diagnostics;

    using Serilog.Core;
    using Serilog.Events;
 
    public sealed class TraceEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;
            if (activity is null) return;

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToHexString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToHexString()));
        }
    }
}

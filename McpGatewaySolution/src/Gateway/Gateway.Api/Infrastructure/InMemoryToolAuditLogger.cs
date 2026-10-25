namespace Gateway.Api.Infrastructure
{
    using System.Collections.Concurrent;
     
    using Gateway.Core.Abstractions;
    using Gateway.Core.Contracts;

    public sealed class InMemoryToolAuditLogger : IToolAuditLogger
    {
        private readonly ConcurrentQueue<ToolInvocationRecord> _records = new();

        public Task LogAsync(ToolInvocationRecord record, CancellationToken cancellationToken)
        {
            _records.Enqueue(record);
            return Task.CompletedTask;
        }

        public IReadOnlyList<ToolInvocationRecord> GetRecords()
        {
            return _records.ToArray();
        }

        public PagedAuditResult GetPagedRecords(
            int page, int pageSize,
            string? outcome = null,
            string? toolName = null,
            string? subjectId = null,
            string? search = null)
        {
            var snapshot = _records.ToArray();

            IEnumerable<ToolInvocationRecord> query = snapshot;

            if (!string.IsNullOrWhiteSpace(outcome))
                query = query.Where(r => r.Outcome.Equals(outcome, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(toolName))
                query = query.Where(r => r.ToolName.Contains(toolName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(subjectId))
                query = query.Where(r => r.SubjectId.Contains(subjectId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.ToolName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (r.UserEmail?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.DownstreamService?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.TraceId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var ordered = query.OrderByDescending(r => r.Timestamp).ToList();
            var totalCount = ordered.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedAuditResult(items, totalCount, page, pageSize, totalPages);
        }
    }
}

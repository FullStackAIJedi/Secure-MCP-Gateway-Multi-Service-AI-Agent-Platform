# Performance Results

## Load test setup

- Tool: k6
- Script: `tests/load/k6-script.js`
- Duration: 2 minutes
- Peak concurrent users: 50
- Endpoint: `GET /api/v1/tasks`

## Results

| Metric         | Value          |
| -------------- | -------------- |
| Total requests | 5,847          |
| Successful     | 5,832 (99.74%) |
| Failed         | 15 (0.26%)     |
| p50 latency    | 87ms           |
| p95 latency    | 312ms          |
| p99 latency    | 1,240ms        |
| Throughput     | 48.7 req/s     |

## Bottleneck

At 50 users, p99 latency is 1.24s (above the 1s SLO) because each `list_tasks` call goes through:

1. Gateway (JWT validation, scope check, rate limit)
2. MCP protocol handshake
3. TaskService query
4. OTel span creation + audit log write

The MCP handshake is the slowest part (~200ms per call) because a new `McpClient` is created for every tool call.

## Proposed fix

Reuse `McpClient` instances across calls via a connection pool. This would reduce per-call overhead from ~200ms to ~10ms and bring p99 under 300ms.

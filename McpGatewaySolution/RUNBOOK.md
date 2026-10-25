# Runbook

Operational reference for on-call engineers.

## Health endpoints

| Endpoint        | Purpose                | Expected |
| --------------- | ---------------------- | -------- |
| `/health/live`  | Process is running     | 200 OK   |
| `/health/ready` | Ready to serve traffic | 200 OK   |
| `/health`       | Full health check      | 200 OK   |

## Common alerts

### 5xx error rate > 5% for 5 minutes

1. Check `/health/ready` on all services.
2. Check OTel traces in Aspire Dashboard for the failing service.
3. Check PostgreSQL connectivity: `docker compose exec postgres pg_isready`.
4. Check OTel Collector logs: `docker compose logs --tail=100 otel-collector`.
5. If a specific downstream service is down, restart it: `docker compose restart taskservice`.

### p95 tool latency > 2s for 10 minutes

1. Check `mcp.tool.execution.duration` metric in Aspire Dashboard.
2. Identify the slow tool.
3. Check downstream service logs for slow queries.
4. Check PostgreSQL `pg_stat_activity` for long-running queries.

### Rate limit 429 spike

1. Check `X-RateLimit-Remaining` header in client logs.
2. Identify the user (from audit log `/api/v1/security/audit-logs`).
3. If legitimate, increase limit in `Gateway/Program.cs`.
4. If abuse, block the user in PostgreSQL.

### OTel Collector down

1. Check `docker compose ps otel-collector`.
2. Check logs: `docker compose logs --tail=100 otel-collector`.
3. Restart: `docker compose restart otel-collector`.
4. Traces will be lost during downtime; metrics will resume after restart.

## Rollback procedure

1. Identify the last known good commit: `git log --oneline`.
2. Checkout: `git checkout <commit>`.
3. Rebuild: `docker compose up -d --build`.
4. Verify: `curl http://localhost:5000/health/ready`.

## Incident response

1. Acknowledge the incident in the on-call channel.
2. Open a GitHub issue with the `incident` template.
3. Mitigate first (restart, rollback), investigate second.
4. Write a post-mortem within 48 hours.

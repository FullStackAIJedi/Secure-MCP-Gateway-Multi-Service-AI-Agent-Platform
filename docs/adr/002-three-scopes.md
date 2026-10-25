# ADR 002: Three scopes instead of one

## Status

Accepted

## Context

A single `mcp.tools` scope would let any authenticated user call any tool, including destructive ones.

## Decision

Split into three scopes:
- `mcp.tools.read` — read-only tools
- `mcp.tools.write` — create/update tools
- `mcp.tools.admin` — delete tools

## Consequences

- Read users cannot accidentally create or delete data.
- Write users cannot delete without explicit admin scope.
- Adding a new tool requires choosing its scope in `ToolRegistry`.
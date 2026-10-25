# ADR 001: Use MCP protocol instead of direct HTTP

## Status

Accepted

## Context

The Gateway needs to call downstream services (TaskService, DocumentService). Options:
1. Direct HTTP calls
2. gRPC
3. MCP protocol

## Decision

Use MCP protocol.

## Consequences

- Downstream services expose their tools as MCP tools, not HTTP endpoints.
- The Gateway can use the same MCP client library for all downstream services.
- Tool schemas are self-describing (MCP tools/list).
- Adds ~200ms per call for MCP handshake.
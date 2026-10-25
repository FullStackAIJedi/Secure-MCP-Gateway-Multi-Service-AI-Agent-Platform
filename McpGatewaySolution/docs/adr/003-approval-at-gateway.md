# ADR 003: Approval workflow at the Gateway

## Status

Accepted

## Context

Destructive operations should require human approval. Where should approval live?
1. At the Agent (before calling the tool)
2. At the Gateway (as part of tool execution)
3. At the React client

## Decision

Approval at the Gateway. Destructive tools return `APPROVAL_REQUIRED:tool:arguments` instead of executing. The Agent detects this and returns a structured approval request. When approved, the client calls a Gateway endpoint that executes the operation.

## Consequences

- Cannot be bypassed by a compromised LLM.
- Approval state is not stored; each approval is a one-time call.
- Adds two new Gateway endpoints (`/api/internal/v1/delete-task`, `/api/internal/v1/delete-document`).
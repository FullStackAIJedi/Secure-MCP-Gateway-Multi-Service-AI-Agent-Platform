# ADR 004: OTel redaction at the collector

## Status

Accepted

## Context

Observability backends are often less protected than application databases. JWTs and emails must not appear in traces.

## Decision

Use the `redaction` processor in OTel Collector with `allow_all_keys: false` and an explicit allow-list. Regex blocks JWTs, emails, and credit card numbers.

## Consequences

- Only ~25 attributes reach Aspire Dashboard.
- Content capture disabled via `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=false`.
- Adding a new span attribute requires updating the allow-list.
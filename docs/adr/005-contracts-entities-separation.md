# ADR 005: Separate contracts and entities

## Status

Accepted

## Context

EF entities have navigation properties and internal state. Returning them directly from APIs risks exposing internal structure.

## Decision

All DTOs live in `*/Contracts/` folders. All EF entities live in `*/Entities/` folders.

## Consequences

- APIs never return entities directly.
- Adding a field to an entity requires a conscious decision whether to add it to the contract.
- More files, but clearer boundaries.
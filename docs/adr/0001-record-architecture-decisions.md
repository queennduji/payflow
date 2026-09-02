# 1. Record architecture decisions

Date: 2026-08-21

## Status

Accepted

## Context

PayFlow is built in phases, several of which replace an earlier phase's approach (synchronous
orchestration → saga, in-process idempotency → persisted saga state, etc.). Without a written
record, it's easy for a reader (or a future contributor) to mistake an intentional, superseded
simplification for an oversight – or to re-litigate a decision that was already made deliberately.

## Decision

We will use Architecture Decision Records (ADRs), one Markdown file per decision, numbered
sequentially in `docs/adr/`, following the lightweight format Michael Nygard proposed. Each
non-trivial architectural choice – and, importantly, each deliberately deferred one – gets a
record: what we decided, why, and what we didn't choose.

## Consequences

Anyone reviewing this repo can answer "why is it built this way?" from `docs/adr/` instead of
guessing from the code alone. Superseded decisions are marked as such rather than deleted, so the
history of the design stays legible.

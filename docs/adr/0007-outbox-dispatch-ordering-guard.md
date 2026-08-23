# 7. A consistency guard in front of the saga's response bridge

Date: 2026-08-22

## Status

Accepted

## Context

Load-testing [ADR-0006](0006-synchronous-facade-over-async-saga.md)'s request/response bridge
turned up a real ordering bug: under concurrent load, `POST /payments` would occasionally return
`201 Created` with a *stale* payment status (e.g. `Pending` instead of the saga's actual outcome),
even though a `GET /payments/{id}` moments later showed the correct value. This was not a caching
artifact — it reproduced with a brand-new, untracked `DbContext` reading immediately after the
saga's response was observed.

The root cause: MassTransit's EF Core saga repository (`EntityFrameworkSagaRepositoryContextFactory`)
wraps a message's entire consumer pipeline — including every activity the state machine runs — in
one explicit database transaction (Postgres `SERIALIZABLE`, which is also why concurrent saga
activity legitimately produces retriable `40001` conflicts; see the `UseMessageRetry` policy in each
service's `Program.cs`). That transaction only truly commits once the whole pipeline returns.
`AddEntityFrameworkOutbox(...).UseBusOutbox()`'s "dispatch immediately after `SaveChanges`" fast
path, however, can fire — in practice, was observed to fire — before that *outer* commit lands,
because the relevant `SaveChanges` calls happen while still nested inside the saga repository's
still-open transaction. A message published through the outbox during that window (in this case,
`PaymentOutcomeReady`) can reach its consumer before the status change that produced it is durable.

Two structural changes came out of chasing this (both still in the code, both correct on their own
merits) but neither fully closed the gap on their own:
- The `MarkPayment*Command` handlers (`Payflow.Payments.Application.Payments.MarkPaymentCommands`)
  never call `SaveChangesAsync` themselves — they mutate the tracked `Payment` and let the saga
  repository's own commit persist it, so there's exactly one savepoint per message instead of a
  nested one.
- The response bridge is a dedicated message (`PaymentOutcomeReady`) and consumer
  (`PaymentOutcomeReadyConsumer`), not a direct send from inside the saga's activity chain — so the
  actual reply to the waiting `IRequestClient` is at least one full message hop removed from the
  transaction that produced it.

## Decision

`PaymentOutcomeReadyConsumer` confirms the payment's status is actually visible — a plain,
independent, untracked read — before sending the reply, retrying briefly (up to 20 × 50ms) if it
isn't yet. This is a deliberate consistency guard at the one point where an ordering slip would
otherwise become visible to a client, not a workaround for a bug in this codebase's own logic.

## Consequences

Verified under concurrent load (bursts of 5–8 simultaneous requests against the same merchant):
zero instances of a stale status being returned as a final answer. Under heavy enough contention a
request can still exhaust the guard's brief retry budget — at that point it correctly falls back to
`202 Accepted` (ADR-0006) rather than ever answering with data that hasn't landed. Slow-but-honest
over fast-but-wrong.

This is scoped to the one place it was observed (the saga's response bridge). It's a narrow,
targeted fix, not a general policy — if the same interaction surfaces elsewhere as the system
grows, it's worth asking whether it's cheaper to fix at the source (e.g., filing/patching the
outbox-vs-saga-transaction ordering upstream) rather than adding another guard.

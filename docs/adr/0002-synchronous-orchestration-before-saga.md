# 2. Synchronous HTTP orchestration in Phase 1, saga in Phase 2

Date: 2026-08-21

## Status

Superseded by [ADR-0005](0005-saga-orchestration-and-outbox.md), which implements the fix described below.

## Context

The core payment flow (authorize → capture → post to ledger) touches three services. There are two
ways to build this from the start: a message-broker-backed saga with a transactional outbox, or a
synchronous call chain from Payments to Authorization and Ledger. The saga is the architecturally
correct answer for production – it's also the flagship pattern this project exists to demonstrate.

## Decision

Phase 1 deliberately ships the naive synchronous version first: `Payments.Api` calls Authorization,
then Ledger, over plain HTTP, inside one MediatR command handler
(`SubmitPaymentCommandHandler`). This is not a placeholder we forgot to fix – it is the concrete,
runnable illustration of the problem sagas exist to solve.

## Consequences

**Known gap, left in on purpose:** if the process crashes or the Ledger call fails *after*
Authorization has already approved the charge, Phase 1 marks the payment `Failed` and does not
retry – an operator would need to manually reconcile that authorization (see
`SubmitPaymentCommandHandler.Handle`, the `ledgerResponse.Posted` branch). A client retrying with
the same Idempotency-Key on a fresh `Failed` payment causes a *second* authorization request rather
than resuming the first attempt, because Phase 1 has no durable record of "where" a payment got to
beyond the `Payment` aggregate's own status.

Two things partially mitigate this without solving it: both Authorization and Ledger are built as
idempotent receivers keyed by `PaymentId` (Authorization via `IAuthorizationStore`, Ledger via a
unique index on `PaymentId`), so at least a *retried call for the same attempt* can't double-charge
or double-post. What they can't fix is Payments' own inability to resume a half-finished attempt.

Phase 2 replaces the in-handler orchestration with a MassTransit saga state machine plus a
transactional EF Core outbox: each step becomes a durable state transition with a compensating
action, so a crash mid-flow resumes (or cleanly compensates) instead of leaving a stuck payment.
That phase is the actual fix; this ADR exists so the gap reads as "the next phase's motivation," not
as a bug.

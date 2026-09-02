# 5. Saga orchestration (MassTransit) with a transactional outbox

Date: 2026-08-21

## Status

Accepted

## Context

[ADR-0002](0002-synchronous-orchestration-before-saga.md) shipped the Phase 1 gap on purpose: a
crash between authorizing a payment and posting it to the ledger left no durable record of where
the attempt got to, and a client retry started a *second* authorization rather than resuming the
first. Fixing this for real means the payment flow's state has to live somewhere durable between
steps, and messages between services have to survive a process crash – that's a saga with a
transactional outbox, not a bigger try/catch.

## Decision

`Payflow.Payments.Application.Saga.PaymentSagaStateMachine` (MassTransit) orchestrates
`Submitted → CheckingFraud → Authorizing → PostingLedger → (Completed | Declined | Failed)`, with
Fraud, Authorization, and Ledger each becoming a message consumer that does its existing work (the
same `AuthorizePaymentCommandHandler`/`PostLedgerEntryCommandHandler` from Phase 1, called from a
consumer instead of an HTTP endpoint) and publishes a result event back to the saga.

- **Saga state persists via EF Core** against each service's own existing DbContext (no new
  database) – `PaymentsDbContext` for the saga instance itself. This is what makes the
  orchestration resumable: the saga's current step is a database row, not a call stack.
- **Every service that publishes a message does so via MassTransit's transactional outbox**
  (`AddEntityFrameworkOutbox`) – the outgoing message and the database write it's conditioned on
  commit in the same transaction. A crash after commit can't lose the message; a crash before
  commit can't leak one that never logically happened.
- **Compensating transaction:** if Ledger posting fails after authorization already succeeded, the
  saga sends `VoidAuthorization` and waits for `AuthorizationVoided` before finalizing the payment
  as `Failed` – see `PaymentSagaStateMachine`'s `PostingLedger`/`VoidingAuthorization` states. This
  is the concrete fix for the gap ADR-0002 documented.
- **Authorization gained a real Postgres-backed idempotent-consumer store**, replacing Phase 1's
  documented `InMemoryAuthorizationStore` simplification – closing the other gap ADR-0002 flagged
  (that store not surviving a restart or working across replicas).

## Consequences

The Payment aggregate's own status column is still the single source of truth for "what happened
to this payment" – the saga drives it via small `MarkPayment*Command`s (`MarkPaymentAuthorizedCommand`,
etc.) rather than duplicating status externally. `GET /payments/{id}` doesn't need to know a saga
exists.

`POST /payments`'s HTTP contract needed a deliberate answer once the work became asynchronous –
see [ADR-0006](0006-synchronous-facade-over-async-saga.md).

Testing moved from mocking `HttpClient` calls to MassTransit's in-memory `ITestHarness`: the saga's
full state machine, including the compensating transaction, is tested with no broker and no
database – see `Payflow.Payments.UnitTests/Saga/PaymentSagaStateMachineTests.cs`.

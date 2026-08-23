# 6. A synchronous facade over the asynchronous saga

Date: 2026-08-21

## Status

Accepted

## Context

Once the payment flow (ADR-0005) became message-driven, `POST /payments` needed a real answer to
"what does the HTTP response look like now?" Two options: make the endpoint genuinely async (return
`202 Accepted` immediately, client polls `GET /payments/{id}`), or preserve the Phase 1 experience
(block and return the final status) by bridging the synchronous HTTP call onto the async saga. The
user's call: keep the existing contract, since the common case (fraud check → authorize → post,
all fast) has no reason to force every client into a poll loop.

## Decision

`Payments.Api` sends `ProcessPayment` via `IRequestClient<ProcessPayment>` and awaits a
`PaymentProcessed` response with a bounded timeout (10s). The saga's `Initially()` handler stashes
the request's `ResponseAddress`/`RequestId` into saga state; whichever step actually finalizes the
saga — Fraud rejecting, Authorization declining, or Ledger posting successfully — publishes a
`PaymentOutcomeReady` message carrying that stashed address, several messages after the original
request arrived. A dedicated `PaymentOutcomeReadyConsumer` is what actually sends the reply, rather
than the saga's own activity chain directly, so it can guard against the outbox/saga-transaction
ordering issue noted on that consumer. This overall shape — stash the requester's address in saga
state, respond from wherever the saga finalizes — is a documented MassTransit pattern for
long-running request/response, not a workaround.

If the saga hasn't finished within the timeout, the endpoint falls back to `202 Accepted` with a
`Location: /payments/{id}` header instead of blocking indefinitely.

The idempotency race this replaces (ADR-0002's "a retry starts a second authorization") is resolved
independently of the response bridge: `Payment` keeps its unique index on
`(MerchantId, IdempotencyKey)`, so a concurrent retry's insert fails, is translated to
`PaymentAlreadyInFlightException`, and the handler reports the *existing* payment — replaying it if
terminal, or itself falling back to `202` if the original attempt is still in flight. See
`SubmitPaymentCommandHandler`.

## Consequences

Clients get the Phase 1 experience for the common case with none of the internal fragility. Clients
that hit the slow path get a standard async contract (`202` + polling) rather than a hung
connection. The trade-off is complexity: the saga has to carry response-routing state it wouldn't
otherwise need, and a bug in the response path would silently strand a waiting HTTP client until its
own timeout — worth calling out explicitly rather than leaving implicit in the code.

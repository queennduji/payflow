# Architecture

This document tracks the system's shape as it grows phase by phase (see the [README](../README.md)
for the full roadmap). It reflects **Phase 1**: a synchronous vertical slice, intentionally not yet
the saga-based design that Phase 2 introduces — see [ADR-0002](adr/0002-synchronous-orchestration-before-saga.md).

## Container view (Phase 1)

```mermaid
flowchart LR
    client([Client])

    subgraph payflow[PayFlow]
        gateway[["Gateway\n(YARP)"]]
        payments[["Payments.Api"]]
        auth[["Authorization.Api\n(mock processor)"]]
        ledger[["Ledger.Api"]]
        paymentsDb[(payments_db)]
        ledgerDb[(ledger_db)]
    end

    client -->|"POST /api/payments\nIdempotency-Key: ..."| gateway
    gateway --> payments
    payments -->|"POST /authorize"| auth
    payments -->|"POST /entries"| ledger
    payments --- paymentsDb
    ledger --- ledgerDb
    client -->|"GET /api/ledger/accounts/{id}/balance"| gateway
    gateway --> ledger
```

## Request flow: `POST /payments`

```mermaid
sequenceDiagram
    participant C as Client
    participant P as Payments.Api
    participant A as Authorization.Api
    participant L as Ledger.Api

    C->>P: POST /payments (Idempotency-Key)
    P->>P: Idempotency lookup (merchant, key)
    alt already handled
        P-->>C: replay cached response
    else new request
        P->>P: Payment.Submit() -> Pending
        P->>A: POST /authorize (PaymentId, amount, ...)
        alt declined
            A-->>P: Approved=false
            P->>P: Payment.Decline()
            P-->>C: 201 Created (status: Declined)
        else approved
            A-->>P: Approved=true, AuthorizationId
            P->>P: Payment.Authorize()
            P->>L: POST /entries (PaymentId, debit/credit accounts)
            alt ledger post fails
                L-->>P: Posted=false
                P->>P: Payment.Fail() — not cached, see ADR-0002
                P-->>C: 201 Created (status: Failed)
            else posted
                L-->>P: Posted=true
                P->>P: Payment.Capture()
                P-->>C: 201 Created (status: Captured)
            end
        end
    end
```

## Bounded contexts

| Service | Owns | Notes |
|---|---|---|
| Payments | `Payment` aggregate, idempotency records | Entry point; orchestrates the flow (Phase 1) / hosts the saga (Phase 2) |
| Authorization | Mock authorization decisions | Idempotent per `PaymentId`; in-memory store is a documented Phase-1 simplification |
| Ledger | `Account`, `LedgerEntryGroup` (double-entry) | Balances are always derived, never stored — [ADR-0003](adr/0003-derived-ledger-balances.md) |

Each service follows Clean Architecture layering (`Domain` → `Application` → `Infrastructure` →
`Api`) with MediatR for CQRS inside `Application`. Cross-service DTOs live in
`Payflow.Shared.Contracts`; framework-free domain building blocks (`Entity`, `AggregateRoot`,
`ValueObject`, `Result`, `Money`) live in `Payflow.Shared.Kernel`.

## Roadmap

See the [README](../README.md#roadmap) for the phase-by-phase plan (saga + outbox, resilience
engineering, observability, Kubernetes/Helm, security, load/chaos testing). This document will grow
a diagram per phase as each lands.

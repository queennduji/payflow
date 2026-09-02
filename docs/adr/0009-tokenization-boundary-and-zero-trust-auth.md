# 9. A tokenization boundary and per-service JWT validation

Date: 2026-08-28

## Status

Accepted

## Context

Phase 6 adds real auth (Keycloak OIDC) and a tokenization vault. Two separate questions this
answers: where does a raw card number stop existing in this system, and which layer is responsible
for checking that a caller is who they say they are.

## Decision

**Tokenization boundary:** `Vault.Api`'s `POST /vault/tokenize` is the only place a full card number
is ever accepted. `VaultToken.Issue` takes the number in, computes the last 4 digits, and returns –
the full number is never assigned to a field, logged, or persisted anywhere, not even encrypted.
Everything downstream of that boundary (Payments, Authorization, the saga) only ever sees the
resulting `tok_...` reference, exactly as it has since Phase 1's `paymentMethodRef`. This keeps the
cardholder-data-handling surface to one small, independently-deployable service rather than spread
across the platform.

**Per-service auth, not gateway-only:** every service – not just the gateway – validates its own JWT
against Keycloak. The message bus is the internal trust boundary; every HTTP-facing endpoint is a
perimeter that checks its own credentials rather than trusting the network path a request arrived
on. A compromised or misconfigured route at the gateway can't turn into an open door to a service
behind it.

## Consequences

No real card data ever needs a PCI-scoped storage or encryption story in this codebase – there's
nothing to protect because nothing is stored. Auth failures are caught at the earliest possible
point, and a service reachable directly (bypassing the gateway, e.g. from inside the cluster) is
just as protected as one reached through it. The cost: seven services each do their own token
validation instead of one shared enforcement point, and role-based authorization (Keycloak realm
roles as ASP.NET Core role claims) is not implemented this phase – "authenticated" is the boundary
enforced now; a follow-up need, not an oversight.

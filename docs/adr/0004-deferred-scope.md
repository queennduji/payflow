# 4. Deferred scope: Kafka/event sourcing, service mesh, contract testing

Date: 2026-08-21

## Status

Accepted

## Context

A payments platform can reasonably grow in several directions we chose not to pursue, at least not
yet. Recording them here is the difference between "not done because no one thought of it" and
"considered, and deliberately out of scope for now."

## Decision

- **Kafka / event sourcing.** The saga (Phase 2) uses RabbitMQ via MassTransit for orchestration
  messaging, not Kafka, and the Payment aggregate is state-based (current status + fields), not
  event-sourced (rebuilt by replaying events). RabbitMQ's competing-consumers-plus-saga model is
  the more idiomatic fit for orchestrating a bounded, per-payment workflow; Kafka's log-based model
  earns its complexity for high-volume event streaming or when multiple independent consumers need
  their own replayable view of the same event history — not needed here.
- **Service mesh / mTLS.** Inter-service traffic runs over plain HTTP inside the cluster network in
  every phase of this project, secured by Kubernetes NetworkPolicies (Phase 5) rather than a mesh
  like Istio/Linkerd. A mesh is the right tool once you have enough services and teams that you need
  mTLS, retries, and traffic policy enforced uniformly out of band — for a project this size it
  would mostly add YAML, not signal.
- **Consumer-driven contract testing (Pact).** Cross-service contracts are covered by integration
  tests against real dependencies via Testcontainers (Phase 7), not Pact. Pact earns its cost when
  services are owned by different teams that can't easily run each other's real dependencies
  locally — here, one Testcontainers suite can spin up the real thing.

## Consequences

If this project grows a genuine multi-consumer event-streaming need, or crosses a team boundary
that makes Testcontainers-based integration testing impractical, these should be revisited — each
as its own ADR superseding the relevant bullet above, not as a silent scope change.

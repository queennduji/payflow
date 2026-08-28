# 8. Hand-rolled Postgres/RabbitMQ manifests instead of community chart dependencies

Date: 2026-08-26

## Status

Accepted

## Context

The Helm chart needs to run five Postgres instances and RabbitMQ, alongside the six .NET services.
Community charts (e.g. Bitnami's `postgresql`/`rabbitmq`) are the usual default for this, versus
authoring the StatefulSets/Deployments directly in this chart.

## Decision

Postgres and RabbitMQ are plain StatefulSet/Deployment + Service templates owned by this chart, not
`Chart.yaml` dependencies on a community chart. Each Postgres instance is a single-replica
StatefulSet with a `volumeClaimTemplates`-backed PVC; RabbitMQ is a single-replica Deployment with
no persistent volume, matching the same ephemeral choice `docker-compose.yml` already makes for it.

## Consequences

No external chart repository to add, pin, or have break under us, and no values surface bigger than
what a single-instance, non-HA local deployment actually needs. The cost is real: replication,
backup, and failover that a community chart would provide for free are simply absent here — an
explicit non-goal for a local `kind` demo (see the Phase 5 plan's out-of-scope list), not an
oversight, but a real gap if this chart were ever pointed at a use case that needed them.

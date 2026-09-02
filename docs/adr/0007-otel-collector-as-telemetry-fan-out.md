# 7. An OpenTelemetry Collector between services and the observability backends

Date: 2026-08-23

## Status

Accepted

## Context

Phase 4 adds distributed tracing, metrics, and structured logs, backed by Tempo (traces), Prometheus
(metrics), and Loki (logs). Each service could export straight to all three backends by name, or
every service could send OTLP to one collector that fans out to them.

## Decision

Every service exports OTLP (traces, metrics, logs) to a single `otel-collector` container. The
collector's pipeline config is the only place that knows Tempo, Prometheus, and Loki exist –
application code and `docker-compose.yml`'s service environment variables reference only
`otel-collector`.

## Consequences

Swapping or adding a backend (e.g. a hosted alternative to local Tempo/Loki) is a collector config
change, not a redeploy of six services. The collector is a new single point of failure for
telemetry, not for the payment flow itself: an unreachable collector means missing traces/logs, not
a failed payment, since the OTLP exporters used by both Serilog and the OpenTelemetry SDK degrade to
dropping data rather than blocking the request pipeline.

# Running PayFlow on Kubernetes

The whole platform — six .NET services, five Postgres instances, RabbitMQ, and the observability
stack from [ADR-0007](adr/0007-otel-collector-as-telemetry-fan-out.md) — as one Helm chart, deployed
to a local [kind](https://kind.sigs.k8s.io/) cluster. See
[ADR-0008](adr/0008-hand-rolled-k8s-manifests-over-community-charts.md) for why the chart owns its
own Postgres/RabbitMQ manifests instead of depending on community charts.

## Prerequisites

Docker Desktop, plus `kind`, `helm`, and `kubectl`:

```bash
winget install Kubernetes.kind
winget install Helm.Helm
# kubectl ships with Docker Desktop
```

## 1. Create the cluster

```bash
bash deploy/kind/create-cluster.sh
```

Creates a `payflow` kind cluster and installs ingress-nginx (kind's documented way to expose
services locally — see `deploy/kind/kind-config.yaml`'s port mappings).

## 2. Build and load the images

kind runs its own containerd, separate from your local Docker — it can't see images `docker build`
produces until they're explicitly loaded in.

```bash
bash deploy/kind/build-and-load.sh
```

## 3. Install the chart

```bash
helm install payflow deploy/helm/payflow -n payflow --create-namespace
```

Watch it come up:

```bash
kubectl get pods -n payflow -w
```

All ~17 pods should reach `Running`/`1/1` within a few minutes. The five .NET services that talk to
Postgres/RabbitMQ carry an init container that waits for both before the app itself starts — see the
comment in `templates/services.yaml` for why (Kubernetes has no equivalent to docker-compose's
`depends_on: condition: service_healthy`, and the app's own startup migration isn't retried).

Local-only credentials are pulled from `values.yaml`'s `secrets:` section (same
`local-dev-only` placeholder as `deploy/docker-compose/.env.example`) — override with `--set` or a
gitignored `values.local.yaml` if you ever point this at something that isn't throwaway.

## 4. Run the demo

The gateway is reachable through ingress-nginx at `payflow.local`. Either add it to your hosts file:

```
127.0.0.1 payflow.local
```

or point `curl` at it directly without touching system files:

```bash
curl -s --resolve payflow.local:80:127.0.0.1 -X POST http://payflow.local/api/payments \
  -H "Content-Type: application/json" -H "Idempotency-Key: k8s-demo-1" \
  -d '{"merchantId":"acme","amount":25,"currency":"USD","paymentMethodRef":"tok_visa"}'
```

The rest of the README's [demo walkthrough](../README.md#demo-walkthrough) works the same way —
swap `http://localhost:8080` for `http://payflow.local` (and add `--resolve` if you skipped the
hosts file edit).

## 5. Check it's actually healthy, not just running

```bash
helm test payflow -n payflow
```

Runs a hook Pod that curls the gateway's `/health` through the in-cluster Service.

## 6. Look at the telemetry

```bash
kubectl port-forward -n payflow svc/grafana 3000:3000
```

Open `http://localhost:3000` (`admin` / whatever `secrets.grafanaAdminPassword` resolved to) and use
Explore exactly as described in the README's
[observability walkthrough](../README.md#watching-a-payments-trace-metrics-and-logs) — traces,
metrics, and logs correlate the same way here as they do under `docker compose up`, because it's the
literal same collector/Tempo/Loki/Prometheus/Grafana config, just running as pods instead of
containers.

## Teardown

```bash
helm uninstall payflow -n payflow
kind delete cluster --name payflow
```

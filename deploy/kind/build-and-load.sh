#!/usr/bin/env bash
# Builds all six service images and loads them into the `payflow` kind cluster. kind can't pull
# unpublished local images on its own, so every image built here has to be loaded explicitly –
# this is that step, not a substitute for it.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."

declare -A dockerfiles=(
  [payflow-gateway]="src/Gateway/Payflow.Gateway/Dockerfile"
  [payflow-payments-api]="src/Services/Payments/Payflow.Payments.Api/Dockerfile"
  [payflow-authorization-api]="src/Services/Authorization/Payflow.Authorization.Api/Dockerfile"
  [payflow-ledger-api]="src/Services/Ledger/Payflow.Ledger.Api/Dockerfile"
  [payflow-fraud-api]="src/Services/Fraud/Payflow.Fraud.Api/Dockerfile"
  [payflow-notifications-api]="src/Services/Notifications/Payflow.Notifications.Api/Dockerfile"
)

for image in "${!dockerfiles[@]}"; do
  echo "Building ${image}:local..."
  docker build -f "${dockerfiles[$image]}" -t "${image}:local" .
done

for image in "${!dockerfiles[@]}"; do
  echo "Loading ${image}:local into kind cluster 'payflow'..."
  kind load docker-image "${image}:local" --name payflow
done

echo "All images built and loaded. Next: helm install payflow deploy/helm/payflow -n payflow --create-namespace"

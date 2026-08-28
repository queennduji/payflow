#!/usr/bin/env bash
# Creates the local `payflow` kind cluster with ingress-nginx installed and ready.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

kind create cluster --name payflow --config kind-config.yaml

kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml

echo "Waiting for ingress-nginx to be ready..."
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=180s

echo "Cluster ready. Next: deploy/kind/build-and-load.sh, then helm install."

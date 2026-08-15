#!/usr/bin/env bash
# Create a docker-container Buildx builder (needed for type=gha cache).
# Prefer this over setup-buildx-action to avoid Node action + docker info overhead.
set -euo pipefail

name="${BUILDX_BUILDER_NAME:-soundtrail-ci}"
docker buildx rm -f "$name" >/dev/null 2>&1 || true
docker buildx create \
  --name "$name" \
  --driver docker-container \
  --driver-opt image=moby/buildkit:buildx-stable-1 \
  --buildkitd-flags '--allow-insecure-entitlement security.insecure --allow-insecure-entitlement network.host' \
  --use >/dev/null
docker buildx inspect --bootstrap >/dev/null
echo "BUILDX_BUILDER=${name}" >> "${GITHUB_ENV:-/dev/null}"
echo "Buildx builder ready: ${name}"

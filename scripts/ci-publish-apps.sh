#!/usr/bin/env bash
# Compile Dockerfile.ci via Buildx. On main, also export published apps.
# PRs use --target build + cacheonly so COPY-list drift fails before merge.
set -euo pipefail

if [[ -z "${ACTIONS_RUNTIME_TOKEN:-}" ]]; then
  echo "::error::ACTIONS_RUNTIME_TOKEN unset; type=gha cache-from will fail."
  exit 1
fi

builder="${BUILDX_BUILDER:-${BUILDX_BUILDER_NAME:-soundtrail-ci}}"
target="${DOCKER_BUILD_TARGET:-publish-apps-out}"

args=(
  --builder "$builder"
  --file Dockerfile.ci
  --target "$target"
  --provenance=false
  --sbom=false
  --cache-from type=gha,scope=soundtrail-testhost
)
if [[ "$target" == "build" ]]; then
  args+=(--output type=cacheonly)
else
  mkdir -p package/publish
  args+=(--output type=local,dest=package/publish)
fi
if [[ "${PUBLISH_CACHE_TO:-false}" == "true" ]]; then
  args+=(--cache-to type=gha,mode=max,scope=soundtrail-testhost)
fi

echo "Docker build (target=${target} builder=${builder} cache-to=${PUBLISH_CACHE_TO:-false})"
docker buildx build "${args[@]}" .
if [[ "$target" == "build" ]]; then
  echo "Dockerfile.ci compile succeeded."
else
  echo "Published apps to package/publish"
fi

#!/usr/bin/env bash
# Publish apps via the existing Buildx builder (same GHA layer cache as testhost).
set -euo pipefail

if [[ -z "${ACTIONS_RUNTIME_TOKEN:-}" ]]; then
  echo "::error::ACTIONS_RUNTIME_TOKEN unset; type=gha cache-from will fail."
  exit 1
fi

mkdir -p package/publish
builder="${BUILDX_BUILDER:-${BUILDX_BUILDER_NAME:-soundtrail-ci}}"

args=(
  --builder "$builder"
  --file Dockerfile.ci
  --target publish-apps-out
  --output type=local,dest=package/publish
  --provenance=false
  --sbom=false
  --cache-from type=gha,scope=soundtrail-testhost
)
if [[ "${PUBLISH_CACHE_TO:-false}" == "true" ]]; then
  args+=(--cache-to type=gha,mode=max,scope=soundtrail-testhost)
fi

echo "Publishing apps (builder=${builder} cache-to=${PUBLISH_CACHE_TO:-false})"
docker buildx build "${args[@]}" .
echo "Published apps to package/publish"

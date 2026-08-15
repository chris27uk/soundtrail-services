#!/usr/bin/env bash
# Load/pull Redis/OSB/Raven (+ aspnet for testhost), then bring sidecars up.
# BuildKit is loaded separately (scripts/ci-load-tooling-images.sh) before Buildx.
# docker save runs after tests (see ci.yml) so it does not contend with HttpClient.
set -euo pipefail

# shellcheck source=ci-image-refs.sh
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/ci-image-refs.sh"

compose=(docker compose -f docker-compose.ci.yml)
tar_path="${SIDECAR_IMAGE_TAR:-/tmp/sidecar-images/sidecars.tar}"
mkdir -p "$(dirname "$tar_path")" reports

cleanup_on_error() {
  touch /tmp/sidecars.failed
}
trap cleanup_on_error ERR

image_present() {
  docker image inspect "$1" >/dev/null 2>&1
}

ensure_image() {
  local ref=$1
  if image_present "$ref"; then
    echo "Already present: $ref"
    return 0
  fi
  echo "Pulling $ref"
  docker pull "$ref"
}

# Tags survive docker load; digest refs do not. Prefer tags for "warm cache" checks.
tags_present() {
  local tag
  for tag in "${CI_SIDECAR_TAGS[@]}"; do
    if ! image_present "$tag"; then
      return 1
    fi
  done
  return 0
}

tar_ok=0
if [[ -f "$tar_path" ]]; then
  tar_size=$(stat -c%s "$tar_path" 2>/dev/null || stat -f%z "$tar_path")
  if (( tar_size >= 10000000 )); then
    echo "Loading cached sidecar images from $tar_path ($tar_size bytes)"
    if docker load -i "$tar_path"; then
      tar_ok=1
    else
      echo "Cached sidecar tar was unusable; pulling instead."
      rm -f "$tar_path"
    fi
  else
    echo "Cached sidecar tar is too small ($tar_size bytes); pulling instead."
    rm -f "$tar_path"
  fi
fi

# ID-only / digest-only tars load layers but drop RepoTags — pull digest pins
# (content-addressed) which also create the short tags compose expects.
if [[ "$tar_ok" -ne 1 ]] || ! tags_present; then
  if [[ "$tar_ok" -eq 1 ]]; then
    echo "Cached sidecar tar missing tag refs; ensuring digest pins (will reseed on miss/new key)."
    touch /tmp/sidecars.need-reseed
  fi
  for ref in "${CI_SIDECAR_IMAGES[@]}"; do
    ensure_image "$ref"
  done
else
  for tag in "${CI_SIDECAR_TAGS[@]}"; do
    echo "Already present: $tag"
  done
fi

echo "Starting sidecars"
# Raven healthcheck is /usr/lib/ravendb/scripts/healthcheck.sh.
# Host curl to /setup/alive is not a 7.2 probe and timed out while the container was already healthy.
"${compose[@]}" up -d --wait --wait-timeout 120 redis openservicebus ravendb

# OpenServiceBus has no image healthcheck; confirm management HTTP before testhost starts.
curl -fsS --retry 20 --retry-delay 1 --retry-all-errors http://127.0.0.1:5300/health >/dev/null
echo "Dependencies ready."
"${compose[@]}" ps
touch /tmp/sidecars.ready
touch /tmp/sidecars.done

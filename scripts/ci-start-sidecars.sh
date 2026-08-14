#!/usr/bin/env bash
# Pull/load CI sidecars, wait until healthy, then optionally docker-save for GHA cache.
# Designed to run in the background while the testhost image builds.
set -euo pipefail

compose=(docker compose -f docker-compose.ci.yml)
tar_path="${SIDECAR_IMAGE_TAR:-/tmp/sidecar-images/sidecars.tar}"
mkdir -p "$(dirname "$tar_path")" reports

cleanup_on_error() {
  touch /tmp/sidecars.failed
}
trap cleanup_on_error ERR

if [[ -f "$tar_path" ]]; then
  echo "Loading cached sidecar images from $tar_path"
  if ! docker load -i "$tar_path"; then
    echo "Cached sidecar tar was unusable; pulling instead."
    rm -f "$tar_path"
    "${compose[@]}" pull redis openservicebus ravendb
  fi
else
  echo "Pulling sidecar images"
  "${compose[@]}" pull redis openservicebus ravendb
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

if [[ ! -f "$tar_path" ]]; then
  echo "Saving sidecar images for cache"
  mapfile -t images < <("${compose[@]}" config --images)
  docker save "${images[@]}" -o "${tar_path}.partial"
  mv "${tar_path}.partial" "$tar_path"
fi

touch /tmp/sidecars.done

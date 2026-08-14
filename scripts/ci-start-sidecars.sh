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
if [[ "$tar_ok" -ne 1 ]]; then
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

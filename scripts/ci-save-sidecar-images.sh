#!/usr/bin/env bash
# Write sidecar + tooling tars when missing (for GHA cache).
set -euo pipefail

# shellcheck source=ci-image-refs.sh
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/ci-image-refs.sh"

dir="${SIDECAR_IMAGE_DIR:-/tmp/sidecar-images}"
sidecar_tar="${SIDECAR_IMAGE_TAR:-$dir/sidecars.tar}"
tooling_tar="${SIDECAR_TOOLING_TAR:-$dir/tooling.tar}"
mkdir -p "$dir"

wrote=0

if [[ ! -f "$sidecar_tar" ]]; then
  echo "Saving sidecar images for cache"
  set +e
  mapfile -t ids < <(docker compose -f docker-compose.ci.yml images -q redis openservicebus ravendb | awk 'NF && !seen[$0]++')
  if (( ${#ids[@]} >= 1 )) && docker save "${ids[@]}" -o "${sidecar_tar}.partial"; then
    mv "${sidecar_tar}.partial" "$sidecar_tar"
    echo "Wrote sidecar tar ($(stat -c%s "$sidecar_tar") bytes)"
    wrote=1
  else
    echo "docker save (sidecars) failed; will skip cache upload if incomplete."
    rm -f "${sidecar_tar}.partial"
  fi
  set -e
else
  echo "Sidecar tar already present; skip docker save."
fi

if [[ ! -f "$tooling_tar" ]]; then
  echo "Saving CI tooling images for cache"
  set +e
  mapfile -t ids < <(
    {
      docker image inspect --format '{{.Id}}' "$CI_ASPNET_IMAGE" 2>/dev/null || true
      docker image inspect --format '{{.Id}}' "$CI_BUILDKIT_IMAGE" 2>/dev/null || true
    } | awk 'NF && !seen[$0]++'
  )
  if (( ${#ids[@]} >= 1 )) && docker save "${ids[@]}" -o "${tooling_tar}.partial"; then
    mv "${tooling_tar}.partial" "$tooling_tar"
    echo "Wrote tooling tar ($(stat -c%s "$tooling_tar") bytes)"
    wrote=1
  else
    echo "docker save (tooling) failed; will skip cache upload if incomplete."
    rm -f "${tooling_tar}.partial"
  fi
  set -e
else
  echo "Tooling tar already present; skip docker save."
fi

if [[ -f "$sidecar_tar" && -f "$tooling_tar" ]]; then
  sidecar_size=$(stat -c%s "$sidecar_tar")
  tooling_size=$(stat -c%s "$tooling_tar")
  echo "sidecar tar bytes=$sidecar_size tooling tar bytes=$tooling_size"
  if (( sidecar_size >= 10000000 && tooling_size >= 1000000 )); then
    echo "cache_ok=true"
    exit 0
  fi
fi

echo "cache_ok=false"
# Non-zero only if we expected to write and could not produce usable tars.
exit 0

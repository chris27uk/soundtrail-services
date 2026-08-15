#!/usr/bin/env bash
# Write sidecar + tooling tars when missing (for GHA cache).
# Save by tag (not digest/ID) so docker load restores RepoTags and compose skips Hub.
set -euo pipefail

# shellcheck source=ci-image-refs.sh
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/ci-image-refs.sh"

dir="${SIDECAR_IMAGE_DIR:-/tmp/sidecar-images}"
sidecar_tar="${SIDECAR_IMAGE_TAR:-$dir/sidecars.tar}"
tooling_tar="${SIDECAR_TOOLING_TAR:-$dir/tooling.tar}"
mkdir -p "$dir"

if [[ ! -f "$sidecar_tar" ]]; then
  echo "Saving sidecar images for cache (by tag)"
  set +e
  if docker save "${CI_SIDECAR_TAGS[@]}" -o "${sidecar_tar}.partial"; then
    mv "${sidecar_tar}.partial" "$sidecar_tar"
    echo "Wrote sidecar tar ($(stat -c%s "$sidecar_tar") bytes)"
  else
    echo "docker save (sidecars) failed; will skip cache upload if incomplete."
    rm -f "${sidecar_tar}.partial"
  fi
  set -e
else
  echo "Sidecar tar already present; skip docker save."
fi

if [[ ! -f "$tooling_tar" ]]; then
  echo "Saving CI tooling image for cache (BuildKit by tag)"
  set +e
  if docker save "$CI_BUILDKIT_IMAGE" -o "${tooling_tar}.partial"; then
    mv "${tooling_tar}.partial" "$tooling_tar"
    echo "Wrote tooling tar ($(stat -c%s "$tooling_tar") bytes)"
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
  # BuildKit-only tooling tar is much smaller than the old aspnet+buildkit bundle.
  if (( sidecar_size >= 10000000 && tooling_size >= 100000 )); then
    echo "cache_ok=true"
    exit 0
  fi
fi

echo "cache_ok=false"
exit 0

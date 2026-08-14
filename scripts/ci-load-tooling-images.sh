#!/usr/bin/env bash
# Sync load/pull BuildKit + aspnet before setup-buildx (avoids Hub pull during bootstrap).
set -euo pipefail

# shellcheck source=ci-image-refs.sh
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/ci-image-refs.sh"

tar_path="${SIDECAR_TOOLING_TAR:-/tmp/sidecar-images/tooling.tar}"
mkdir -p "$(dirname "$tar_path")"

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

if [[ -f "$tar_path" ]]; then
  tar_size=$(stat -c%s "$tar_path" 2>/dev/null || stat -f%z "$tar_path")
  if (( tar_size >= 1000000 )); then
    echo "Loading CI tooling images from $tar_path ($tar_size bytes)"
    if ! docker load -i "$tar_path"; then
      echo "Tooling tar was unusable; pulling instead."
      rm -f "$tar_path"
    fi
  else
    echo "Tooling tar too small ($tar_size bytes); pulling instead."
    rm -f "$tar_path"
  fi
fi

ensure_image "$CI_BUILDKIT_IMAGE"
ensure_image "$CI_ASPNET_IMAGE"

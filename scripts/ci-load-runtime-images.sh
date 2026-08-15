#!/usr/bin/env bash
# Load/pull aspnet runtime image for compose run testhost.
# Signals /tmp/runtime.loaded on success.
set -euo pipefail

# shellcheck source=ci-image-refs.sh
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/ci-image-refs.sh"

tar_path="${SIDECAR_RUNTIME_TAR:-/tmp/sidecar-images/runtime.tar}"
mkdir -p "$(dirname "$tar_path")"
rm -f /tmp/runtime.loaded /tmp/runtime.load-failed

cleanup_on_error() {
  touch /tmp/runtime.load-failed
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

pin_tag_from_digest() {
  local digest_ref=$1
  local tag=$2
  if image_present "$tag"; then
    return 0
  fi
  local id
  id=$(docker image inspect -f '{{.Id}}' "$digest_ref")
  docker tag "$id" "$tag"
  echo "Pinned $tag <- $digest_ref"
}

tar_ok=0
if [[ -f "$tar_path" ]]; then
  tar_size=$(stat -c%s "$tar_path" 2>/dev/null || stat -f%z "$tar_path")
  if (( tar_size >= 100000 )); then
    echo "Loading CI runtime image from $tar_path ($tar_size bytes)"
    if docker load -i "$tar_path"; then
      tar_ok=1
    else
      echo "Runtime tar was unusable; pulling instead."
      rm -f "$tar_path"
    fi
  else
    echo "Runtime tar too small ($tar_size bytes); pulling instead."
    rm -f "$tar_path"
  fi
fi

if [[ "$tar_ok" -ne 1 ]] || ! image_present "$CI_ASPNET_TAG"; then
  if [[ "$tar_ok" -eq 1 ]]; then
    echo "Runtime tar missing tag ref; ensuring digest pin (will reseed)."
    touch /tmp/runtime.need-reseed
  fi
  ensure_image "$CI_ASPNET_IMAGE"
  pin_tag_from_digest "$CI_ASPNET_IMAGE" "$CI_ASPNET_TAG"
else
  echo "Already present: $CI_ASPNET_TAG"
fi

touch /tmp/runtime.loaded
echo "Runtime image loaded."

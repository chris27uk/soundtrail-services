#!/usr/bin/env bash
# Prepare CI images — kept for local/manual use.
# Workflow: tooling load overlaps sidecar restore; then load+up bg overlaps Buildx; runtime separate.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$root/ci-load-tooling-images.sh"
bash "$root/ci-load-runtime-images.sh"
bash "$root/ci-start-sidecars-bg.sh"

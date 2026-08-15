#!/usr/bin/env bash
# Prepare CI images — kept for local/manual use.
# Workflow: sidecars restore → load bg → tooling restore → wait load → tooling load → up bg → runtime.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$root/ci-load-sidecar-images.sh"
bash "$root/ci-load-tooling-images.sh"
bash "$root/ci-load-runtime-images.sh"
bash "$root/ci-start-sidecars-bg.sh"

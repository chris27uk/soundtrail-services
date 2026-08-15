#!/usr/bin/env bash
# Prepare CI images — kept for local/manual use.
# Workflow uses: ci-load-tooling-bg → (sidecar restore) → ci-wait-tooling → ci-start-sidecars-bg
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$root/ci-load-tooling-images.sh"
bash "$root/ci-start-sidecars-bg.sh"

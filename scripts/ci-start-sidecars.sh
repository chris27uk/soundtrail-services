#!/usr/bin/env bash
# Load/pull Redis/OSB/Raven then bring sidecars up (local/manual).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$root/ci-load-sidecar-images.sh"
bash "$root/ci-up-sidecars.sh"

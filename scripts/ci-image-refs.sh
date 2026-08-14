# Shared CI image refs (sourced by start/save/wait helpers).
# Keep digests aligned with docker-compose.ci.yml / Dockerfile.ci.

CI_ASPNET_IMAGE="${CI_ASPNET_IMAGE:-mcr.microsoft.com/dotnet/aspnet:10.0.9-noble@sha256:7644f992230d35cf230017189d4038c0ae0f7388b13f4f7ae1900a155bafb597}"
# Pulled by docker/setup-buildx-action (docker-container driver). Cached so bootstrap skips Hub.
CI_BUILDKIT_IMAGE="${CI_BUILDKIT_IMAGE:-moby/buildkit:buildx-stable-1}"

# Sidecar + testhost runtime (saved/loaded with tags so compose does not re-pull).
CI_REDIS_IMAGE="${CI_REDIS_IMAGE:-redis:7-alpine@sha256:9702d01c1f10c3ea9f48211b4362e44f154ff02d063e6f7268eba804059f53bf}"
CI_OPENSERVICEBUS_IMAGE="${CI_OPENSERVICEBUS_IMAGE:-mauritsarissen/openservicebus:latest@sha256:72ec683f93d8de419b58030a2652d4065f3aa8fac77d3c1f7f468c860c5af3cd}"
CI_RAVENDB_IMAGE="${CI_RAVENDB_IMAGE:-ravendb/ravendb:7.2.5-ubuntu.24.04-x64@sha256:d8f45d0eed364f79235d36c64d7f74d01185b17c0d23b74b8515e0cba848dc6d}"

CI_SIDECAR_IMAGES=(
  "$CI_REDIS_IMAGE"
  "$CI_OPENSERVICEBUS_IMAGE"
  "$CI_RAVENDB_IMAGE"
  "$CI_ASPNET_IMAGE"
)

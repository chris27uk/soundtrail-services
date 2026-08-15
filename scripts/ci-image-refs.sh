# Shared CI image refs (sourced by start/save/wait helpers).
# Digests pin content on first pull; tag-only names are what docker save/load and
# compose use (digest refs do not survive docker load → Hub re-pulls).

CI_ASPNET_IMAGE="${CI_ASPNET_IMAGE:-mcr.microsoft.com/dotnet/aspnet:10.0.9-noble@sha256:7644f992230d35cf230017189d4038c0ae0f7388b13f4f7ae1900a155bafb597}"
CI_ASPNET_TAG="${CI_ASPNET_TAG:-mcr.microsoft.com/dotnet/aspnet:10.0.9-noble}"
# Pulled by docker/setup-buildx-action (docker-container driver). Cached so bootstrap skips Hub.
CI_BUILDKIT_IMAGE="${CI_BUILDKIT_IMAGE:-moby/buildkit:buildx-stable-1}"

# Digest pins (ensure/pull). Keep aligned with comments in docker-compose.ci.yml.
CI_REDIS_IMAGE="${CI_REDIS_IMAGE:-redis:7-alpine@sha256:9702d01c1f10c3ea9f48211b4362e44f154ff02d063e6f7268eba804059f53bf}"
CI_OPENSERVICEBUS_IMAGE="${CI_OPENSERVICEBUS_IMAGE:-mauritsarissen/openservicebus:latest@sha256:72ec683f93d8de419b58030a2652d4065f3aa8fac77d3c1f7f468c860c5af3cd}"
CI_RAVENDB_IMAGE="${CI_RAVENDB_IMAGE:-ravendb/ravendb:7.2.5-ubuntu.24.04-x64@sha256:d8f45d0eed364f79235d36c64d7f74d01185b17c0d23b74b8515e0cba848dc6d}"

CI_REDIS_TAG="${CI_REDIS_TAG:-redis:7-alpine}"
CI_OPENSERVICEBUS_TAG="${CI_OPENSERVICEBUS_TAG:-mauritsarissen/openservicebus:latest}"
CI_RAVENDB_TAG="${CI_RAVENDB_TAG:-ravendb/ravendb:7.2.5-ubuntu.24.04-x64}"

# Pull/ensure by digest (content pin).
CI_SIDECAR_IMAGES=(
  "$CI_REDIS_IMAGE"
  "$CI_OPENSERVICEBUS_IMAGE"
  "$CI_RAVENDB_IMAGE"
  "$CI_ASPNET_IMAGE"
)

# Save/load + compose by tag only (RepoTags survive docker load).
CI_SIDECAR_TAGS=(
  "$CI_REDIS_TAG"
  "$CI_OPENSERVICEBUS_TAG"
  "$CI_RAVENDB_TAG"
  "$CI_ASPNET_TAG"
)

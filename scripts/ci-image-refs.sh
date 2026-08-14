# Shared CI image refs (sourced by start/save/wait helpers).
# Keep aspnet digest aligned with Dockerfile.ci RUNTIME_IMAGE / docker-compose.ci.yml.

CI_ASPNET_IMAGE="${CI_ASPNET_IMAGE:-mcr.microsoft.com/dotnet/aspnet:10.0.9-noble@sha256:7644f992230d35cf230017189d4038c0ae0f7388b13f4f7ae1900a155bafb597}"
# Pulled by docker/setup-buildx-action (docker-container driver). Cached so bootstrap skips Hub.
CI_BUILDKIT_IMAGE="${CI_BUILDKIT_IMAGE:-moby/buildkit:buildx-stable-1}"

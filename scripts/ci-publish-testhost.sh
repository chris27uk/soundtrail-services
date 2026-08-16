#!/usr/bin/env bash
# Publish the CI testhost with the pinned runner SDK.
# This avoids pulling a ~418MB BuildKit layer and exporting source-specific cache.
set -euo pipefail

project=tests/Soundtrail.Services.Tests/Soundtrail.Services.Tests.csproj
packages="${NUGET_PACKAGES:-$HOME/.nuget/packages}"

export NUGET_PACKAGES="$packages"
mkdir -p "$packages"
rm -rf testhost
mkdir -p testhost

dotnet restore "$project" \
  --locked-mode \
  --verbosity minimal \
  /p:Configuration=Release \
  /p:UseEmbeddedRaven=false \
  /p:RunAnalyzersDuringBuild=false \
  /p:RunAnalyzers=false \
  /p:EnableNETAnalyzers=false \
  /p:GenerateDocumentationFile=false \
  /p:IsTransformWebConfigInHostBuild=false

dotnet publish "$project" \
  --no-restore \
  --verbosity quiet \
  --output testhost \
  /p:Configuration=Release \
  /p:UseEmbeddedRaven=false \
  /p:ErrorOnDuplicatePublishOutputFiles=false \
  /p:RunAnalyzersDuringBuild=false \
  /p:RunAnalyzers=false \
  /p:EnableNETAnalyzers=false \
  /p:GenerateDocumentationFile=false \
  /p:IsTransformWebConfigInHostBuild=false \
  /p:UseSharedCompilation=true

test -f testhost/Soundtrail.Services.Tests.dll
echo "Published testhost with runner SDK."

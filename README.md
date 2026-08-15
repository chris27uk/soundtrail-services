# Soundtrail.Services

Local-first music catalog discovery services for Soundtrail.

## Aim

Build a C# music catalog API that discovers metadata for a UI and enables track playback through supported streaming providers.

The API is **local-first**: it returns known catalog data quickly from RavenDB and records discovery requests when local data is incomplete. Discovery runs asynchronously. The API never calls MusicBrainz or Odesli directly.

| Concern | Choice |
| --- | --- |
| Canonical metadata | MusicBrainz |
| Playback references | Odesli / Songlink |
| Streaming providers | Apple Music, Spotify, YouTube Music |
| Read model | RavenDB projections |
| Source of truth | Event store |
| Messaging | Azure Service Bus |

Discovery is split intentionally:

- **Orchestrator** — prioritisation, deduplication, lifecycle, dispatching work
- **Worker** — provider admission (budget/leases) and third-party calls
- **Projector** — turns events into RavenDB read models the API serves

See [`specs/music-catalog-discovery-api-v2.md`](specs/music-catalog-discovery-api-v2.md) and related docs under [`specs/`](specs/).

## Solution architecture

```mermaid
flowchart TB
  subgraph clients [Clients]
    UI[Soundtrail UI]
  end

  subgraph runtime [Soundtrail.Services]
    Api[Api]
    Orchestrator[Enrichment.Orchestrator]
    Scheduler[Enrichment.Scheduler]
    Worker[Enrichment.Worker]
    Projector[Projector]
  end

  subgraph shared [Shared libraries]
    Domain[Domain]
    Contracts[Contracts]
    Infra[Infrastructure]
  end

  subgraph data [Data and messaging]
    Raven[(RavenDB)]
    Bus[[Azure Service Bus]]
    Redis[(Redis)]
  end

  subgraph external [External providers]
    MB[MusicBrainz]
    Odesli[Odesli]
  end

  UI -->|HTTP local-first reads| Api
  Api --> Raven
  Api -->|discovery requests| Bus
  Orchestrator --> Bus
  Orchestrator --> Raven
  Scheduler --> Bus
  Scheduler --> Raven
  Worker --> Bus
  Worker --> Redis
  Worker --> MB
  Worker --> Odesli
  Projector --> Bus
  Projector --> Raven

  Api --> Domain
  Api --> Contracts
  Api --> Infra
  Orchestrator --> Domain
  Orchestrator --> Infra
  Worker --> Domain
  Worker --> Infra
  Projector --> Domain
  Projector --> Infra
  Scheduler --> Domain
  Scheduler --> Infra
```

**Runtime hosts** (also wired by Aspire AppHost for local development):

| Project | Role |
| --- | --- |
| `Soundtrail.Services.Api` | Local-first HTTP API (search, artists, albums, tracks, playlists) |
| `Soundtrail.Services.Enrichment.Orchestrator` | Discovery prioritisation and lifecycle |
| `Soundtrail.Services.Enrichment.Scheduler` | Backlog / scheduling host |
| `Soundtrail.Services.Enrichment.Worker` | Provider admission and external lookups |
| `Soundtrail.Services.Projector` | Event → RavenDB read-model projections |

**Shared libraries:** `Soundtrail.Domain`, `Soundtrail.Contracts`, `Soundtrail.Infrastructure`, `Soundtrail.Services.ServiceDefaults`.

## Developer build

Local default is [`build.ps1`](build.ps1) with the SDK in [`global.json`](global.json), RavenDB Embedded, and Testcontainers. Docker is required for Redis and OpenServiceBus when you do not already have those services running.

### Prerequisites

- .NET SDK matching [`global.json`](global.json) (`rollForward: disable`)
- PowerShell 7+ (`pwsh`)
- Docker (integration / end-to-end Testcontainers)

### Host build (local default)

```powershell
./build.ps1 -Restore
```

`-Restore` restores packages then builds and runs the full test pack. Omit `-Restore` only when `project.assets.json` is already present and you want compile/test with `--no-restore`.

Useful switches:

```powershell
./build.ps1 -Clean
./build.ps1 -Restore -TestFilter "FullyQualifiedName~Soundtrail.Services.Tests.Unit"
./build.ps1 -TestFilter "FullyQualifiedName~Soundtrail.Services.Tests.EndToEnd"
./build.ps1 -Configuration Debug
```

End-to-end tests start RavenDB Embedded in-process, WireMock in-process, and Azure Service Bus emulator + Redis via Testcontainers. Docker must be running; you do not need to start compose yourself. Queue names are fixed in code (`ServiceBusQueues`); configure only `ServiceBus:ConnectionString`.

```bash
dotnet test tests/Soundtrail.Services.Tests/Soundtrail.Services.Tests.csproj \
  --filter "FullyQualifiedName~Soundtrail.Services.Tests.EndToEnd"
```

### CI parity (optional)

CI does **not** use Embedded Raven or Testcontainers. It builds a published testhost ([`Dockerfile.ci`](Dockerfile.ci)) and runs it next to Redis, OpenServiceBus, and RavenDB 7.2.5:

```bash
docker build -t soundtrail-testhost:ci --target testhost -f Dockerfile.ci .
mkdir -p reports
docker compose -f docker-compose.ci.yml up -d redis openservicebus ravendb
docker compose -f docker-compose.ci.yml run --rm --no-deps testhost
docker compose -f docker-compose.ci.yml down -v
```

Point a host-run testhost at the same sidecars with `SOUNDTRAIL_TEST_NO_TESTCONTAINERS=1`, `SOUNDTRAIL_TEST_REDIS`, `SOUNDTRAIL_TEST_SERVICEBUS`, and `SOUNDTRAIL_TEST_RAVEN`.

### CI

GitHub Actions uses Buildx + `type=gha` layer cache to build the testhost image from [`Dockerfile.ci`](Dockerfile.ci) (restore → build → publish, no RavenDB.Embedded in the restore graph). Sidecars come from [`docker-compose.ci.yml`](docker-compose.ci.yml). See [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

The PR check is the workflow job **Build \<SemVer\>** (plus a **Test Results** annotation on pull requests). To block merges until it passes, in GitHub go to **Settings → Rules → Rulesets** (or **Branches → Branch protection**) for `main` and require that Build status check.

## Further reading

- [`CODING-STANDARDS.md`](CODING-STANDARDS.md) — solution shape, feature folders, testing approach
- [`specs/`](specs/) — discovery API, prioritisation/admission layering, event sourcing, TrackId

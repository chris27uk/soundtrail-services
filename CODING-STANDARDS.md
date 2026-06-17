# Coding Standards

This document defines the default standards for new feature work in this solution.

The priority is strong business design, clear boundaries, and tests that prove real behavior. Favor the current architecture and the specification over preserving older shapes.

## Primary Principles

- Optimize for the right design, not backwards compatibility with the POC.
- Treat the spec in `specs/` as the source of truth for behavior, naming, and major components.
- Keep business logic technology-independent.
- Preserve ports-and-adapters architecture and vertical slicing.
- Follow outside-in TDD and replace thin/solitary tests with more meaningful coverage as structure matures.
- Add infrastructure through adapters with mandatory fake-and-real integration coverage under the same contract tests.

## Solution Shape

The active projects in this solution are:

- `src/Soundtrail.Contracts`
- `src/Soundtrail.Domain`
- `src/Soundtrail.Services.Api`
- `src/Soundtrail.Services.Application`
- `src/Soundtrail.Services.Catalog.Projector`
- `src/Soundtrail.Services.Domain`
- `src/Soundtrail.Services.Enrichment`
- `src/Soundtrail.Services.Enrichment.Cdc`
- `src/Soundtrail.Services.Enrichment.DiscoveryPlanner`
- `src/Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator`
- `src/Soundtrail.Services.Enrichment.Scheduler`
- `src/Soundtrail.Services.Enrichment.Worker`
- `src/Soundtrail.Services.Infrastructure`

Treat the current repo structure as authoritative unless the solution is deliberately reorganized.

## Dependency Direction

Dependencies must point inward toward business behavior.

- `Contracts` contains shared DTOs and transport-facing shared contracts.
- `Domain` contains shared domain commands, responses, events, aggregates, policies, value objects, and ports.
- application and host projects may depend on `Domain` and `Contracts`
- adapters may depend on infrastructure SDKs
- domain code must not depend on API, Raven, Azure Service Bus, HTTP, serialization, or persistence concerns

If a type exists only to satisfy HTTP, Raven, CDC, queueing, or host startup, it does not belong in the domain layer.

## Domain, Contracts, DTOs, And Documents

Keep domain objects and DTOs clearly separated.

- domain commands live in `src/Soundtrail.Domain/Commands`
- domain responses live in `src/Soundtrail.Domain/Responses`
- domain events live in `src/Soundtrail.Domain/Events`
- shared domain objects live in the domain project
- shared DTOs live in `src/Soundtrail.Contracts`

Rules:

- domain objects are technology-independent and are never serialized
- ports never expose DTOs
- adapters own DTO mapping
- every DTO type must end with `Dto`
- `Document` is reserved for persistence models only
- do not use `Document` as a suffix for shared contracts or transport models

## Aggregates And Event Sourcing

Aggregates are consistency boundaries, not query models.

Rules:

- aggregates should not expose public state for querying
- aggregates should not expose event collections on their public interface
- load/save should occur through aggregate methods and a repository that persists domain events
- repositories map domain events to persisted DTOs through dedicated mapper classes
- aggregate state is rebuilt from domain events only
- projections and status records are read models, not aggregate state

For event-sourced features:

- keep event names business-focused and technology-independent
- keep serialization, Raven document shapes, and stream metadata outside the domain layer
- use optimistic concurrency at the repository boundary
- project lifecycle/read state from events rather than mutating status documents directly

## Feature Placement

Organize code by feature first, then by role inside the feature.

Feature folder rules:

- each feature folder root contains exactly one entrypoint file
- that entrypoint is the handler or other true entrypoint for the feature
- if a folder has no entrypoint, it is not a feature folder and should be a subfolder of a feature folder
- handlers must live in feature folder roots
- adapters live in an `Adapters` folder
- dependency-registration code lives in a feature-local `CompositionRoot` folder
- supporting files live in folders that describe what they do
- `CompositionRoot` folders are not shared between feature folders

Examples of supporting folders:

- `Lookup`
- `Model`
- `Policies`
- `Projection`
- `Indexes`
- `Documents`

Avoid broad utility folders that mix unrelated concerns.

## Layer Responsibilities

### Domain

Use the domain layer for:

- commands
- responses
- events
- aggregates
- value objects
- business rules
- ports

Do not put handlers in the domain project.

### API

Use API projects for:

- HTTP endpoints
- request parsing
- transport-to-domain translation
- HTTP contract mapping
- API-only adapters
- API composition roots

Keep endpoints thin and local-only on the public request path.

### Enrichment Planner, Worker, CDC, Projector, And Coordinator Hosts

Use these projects for:

- feature handlers outside the public request path
- orchestration
- adapter implementations
- queue listeners
- projector subscriptions
- persistence/event-store adapters
- host-specific composition

Keep business decisions in handlers/domain policies, not in listeners or infrastructure wiring.

## Endpoints

Endpoints should:

- parse transport input
- validate basic request shape
- translate to domain commands or requests
- delegate to a handler
- translate domain responses into HTTP contracts

Endpoints should not:

- contain branching business rules
- call infrastructure directly when a handler/port should own the behavior
- perform provider lookups on the public request path

Routing tests should stay minimal:

- one `200` route test per route
- one error-model mapping test where necessary

## Handlers

Handlers are the default use-case entrypoint.

Handlers should:

- model one business use case
- depend on ports, policies, aggregates, and domain abstractions
- return domain/application responses, not HTTP-specific results
- keep control flow explicit and readable

Handlers should not:

- depend on Raven, HTTP, or queue SDK details
- mutate projection/read-model documents directly when an event-sourced workflow should emit events instead
- hide core decisions behind unnecessary abstraction

## Ports And Adapters

Ports define what the business layer needs. Adapters satisfy those ports.

Rules:

- define ports in the domain-owned layer
- keep ports use-case oriented
- do not leak infrastructure SDK types through ports
- do not return DTOs from ports
- adapters may map between domain objects and DTOs/documents

Adapters must be covered by integration tests that exercise:

- the fake implementation
- the real implementation
- the same observable contract under both

This fake-vs-real contract coverage is mandatory.

## Projections And Read Models

Projection/read-model state must stay separate from domain behavior.

Rules:

- read models may be serialized and persisted
- read models are rebuilt from events where the feature is event-sourced
- lifecycle/status documents are projection-only when the aggregate owns the lifecycle
- do not update projection state directly from handlers/listeners if the source of truth is an event stream

## Naming

Follow the naming style already established in the current codebase and spec.

- use clear, literal names
- name handlers as `<UseCase>Handler`
- name commands as nouns or imperative business operations
- name responses as `<UseCase>Response` where appropriate
- name interfaces by role
- name DTOs with the `Dto` suffix
- reserve `Document` for persisted document models only
- prefer business names over technical names like `Key` when the concept is not truly a key

Avoid vague names such as `Helper`, `Utils`, `Manager`, or `Processor` unless the type genuinely matches that abstraction.

## Code Style

- prefer small files with a single clear purpose
- prefer straightforward control flow over indirection
- keep constructors explicit
- use immutability by default where practical
- add comments only when they explain non-obvious intent
- do not introduce inheritance-heavy patterns for application logic or tests

## Testing Standards

### Default Test Strategy

Follow outside-in TDD:

1. start from behavior
2. write the failing test
3. implement the smallest clear change
4. refactor once behavior is proven

### Unit Tests

Default to sociable unit tests.

That means:

- exercise a handler, aggregate, projector, or other meaningful business unit
- use real in-memory collaborators where practical
- use custom fakes for ports
- prove policies through observable behavior when possible

Good sociable test targets:

- handlers
- aggregates and their invariants
- projectors
- orchestration decisions
- request/response decision points

### Solitary Tests

Solitary tests are allowed only when a standalone type has meaningful behavior that would become awkward to prove indirectly.

Examples:

- constrained value objects
- pure normalization rules
- focused event-data mappers where sociable coverage would be noisy

Do not default to solitary tests for policies or handlers when an outside-in or sociable test can prove the same behavior clearly.

Where structure is now in place, prefer replacing solitary tests with outside-in coverage over adding more thin isolated tests.

### Integration Tests

Integration tests should be named after the technology they exercise unless they are minimal routing tests.

Examples:

- `Raven...Tests`
- `AzureServiceBus...Tests`
- `AspNetRouting...Tests`

Rules:

- group integration tests by the technology being tested
- use the test-environment pattern
- each test environment should expose a static factory method
- encapsulate `WebApplicationFactory` inside the test environment class
- keep routing tests minimal

### Adapter Contract Tests

Where a fake represents a real adapter, add one contract suite that runs against:

- the fake implementation
- the real implementation

The same test cases must exercise both implementations.

This is mandatory for adapters and ports used by business logic.

### What New Feature Work Must Test

At minimum, new feature work should cover:

- happy path behavior
- important boundary values
- invalid input handling
- deferred/rejected/failure branches where relevant
- queueing, persistence, projection, replay, deduplication, or concurrency behavior that is part of the business outcome
- fake-vs-real adapter contract coverage for any new adapter pair

For event-sourced features, add coverage for:

- aggregate behavior
- repository load/save/versioning behavior
- projection replay behavior
- outside-in flow where lifecycle state must round-trip through read models

## Public Request Path Rule

The public API path must not directly call third-party providers.

Public requests may:

- read local data
- read local projections
- create demand signals
- append domain events where the public use case owns that command

Provider enrichment must happen asynchronously and under explicit worker/planner control.

## New Feature Checklist

Before finishing a feature, confirm:

- the code sits in the correct project and feature folder
- the feature folder root contains exactly one entrypoint
- handlers are in feature roots
- adapters are in `Adapters`
- composition code is in the feature-local `CompositionRoot`
- domain objects and DTOs are separated correctly
- ports do not leak DTOs or infrastructure types
- business rules live in handlers/domain code, not endpoints or adapters
- event-sourced lifecycle state is projection-driven where applicable
- tests are sociable by default
- outside-in coverage exists where structure supports it
- fake and real adapters share mandatory contract coverage
- the request path remains local-only where required
- the solution builds and relevant tests pass

## Default Verification Commands

Use the smallest command that proves the change.

Common examples:

- `dotnet test tests/Soundtrail.Services.Tests/Soundtrail.Services.Tests.csproj --filter FullyQualifiedName~Unit`
- `dotnet test tests/Soundtrail.Services.Tests/Soundtrail.Services.Tests.csproj --filter FullyQualifiedName~Integration`
- `dotnet test tests/Soundtrail.Services.Tests/Soundtrail.Services.Tests.csproj`

Run focused slices during iteration, then broaden verification when the change touches integration or infrastructure behavior.

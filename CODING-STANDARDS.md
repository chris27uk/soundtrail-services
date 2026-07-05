# Coding Standards

This document defines the default standards for new feature work in this solution.

The goal is consistency more than cleverness. New code should fit the existing shape of the solution, keep the business rules easy to read, and make tests prove behavior rather than implementation details.

## Primary Principles

- Keep the public API request path local, fast, and predictable.
- Put business decisions in application/domain code, not in endpoints or infrastructure adapters.
- Prefer small, explicit types over primitive-heavy code.
- Keep a hard separation between domain models and DTOs.
- Follow outside-in TDD.
- Use custom fakes and sociable unit tests by default.
- Add integration tests where they prove a fake matches a real adapter or where infrastructure behavior matters.
- Do not ship in-memory fakes or no-op adapters inside production app code.

## Solution Shape

The active projects in this solution are:

- `src/Soundtrail.Domain`
- `src/Soundtrail.Contracts`
- `src/Soundtrail.Services.Api`
- `src/Soundtrail.Services.Enrichment.*`

Treat these as the authoritative structure for new work unless the solution is deliberately reorganized.

## Layer Responsibilities

### `Soundtrail.Contracts`

Use this project for shared transport contracts only.

Allowed here:

- shared request DTOs
- shared response DTOs
- shared integration message DTOs
- shared transport enums or identifiers that exist only to support serialization contracts

Rules:

- shared DTO types must end with `Dto`
- shared DTO types must be serialization-friendly and transport-focused
- shared DTOs must not contain business behavior

Not allowed here:

- handlers
- business rules
- persistence documents
- RavenDB or ASP.NET-specific types
- domain models that should live in `Soundtrail.Domain`

### `Soundtrail.Domain`

Use this project for core domain models and business-owned contracts.

Allowed here:

- request/response models
- commands
- events
- domain value types
- core business rules
- small shared abstractions such as ports and ids

Not allowed here:

- handlers
- ASP.NET types
- Raven, Azure Service Bus, or other infrastructure SDK types
- configuration binding
- transport concerns

Foldering rules:

- organize `Soundtrail.Domain` by business area or bounded context first, not by artifact kind
- top-level folders should be business-owned areas such as catalog, discovery, search, enrichment, or shared abstractions
- do not create horizontal catch-all top-level folders such as `Commands`, `Events`, `Responses`, or `Model`
- place commands, events, responses, projections, value types, and ports inside their owning business area
- only genuinely cross-cutting domain infrastructure, such as base abstractions or event-sourcing helpers, may live in a shared abstractions area

### `Soundtrail.Services.Api`

Use this project for HTTP endpoints and infrastructure wiring for the public API.

Allowed here:

- minimal API endpoint definitions
- request parsing and request-to-domain translation
- dependency registration
- infrastructure adapters for ports used by the API
- transport contracts

Not allowed here:

- business rules that belong in handlers or domain types
- direct third-party provider calls on the public search path

### `Soundtrail.Services.Enrichment`

Use this project for enrichment scheduling and related business decisions.

Allowed here:

- scheduling handlers
- prioritization logic
- resolution logic
- queue command models
- persistence and search abstractions needed by scheduling

Not allowed here:

- concrete infrastructure clients
- transport configuration

### `Soundtrail.Services.Enrichment.Scheduler`

Use this project for worker host startup and infrastructure composition.

As worker behavior expands, keep provider orchestration and host concerns here, while keeping reusable business rules in `Soundtrail.Services.Enrichment`.

## Dependency Direction

Dependencies should point inward toward business behavior.

- `Api` may depend on `Soundtrail.Domain`
- `Enrichment.Scheduler` may depend on `Enrichment` and shared core code
- `Enrichment` may depend on `Soundtrail.Domain` where shared message/value types are needed
- core business projects must not depend on API or infrastructure projects
- shared DTOs should depend inward on domain concepts only when that dependency is deliberate and stable
- shared domain objects belong in `Soundtrail.Domain`, not `Soundtrail.Contracts`

If a type only exists to satisfy HTTP, Raven, Azure Service Bus, or host startup, it should not live in a core business project.

Test-only fakes, no-op adapters, and in-memory harnesses must live in test projects, not under `src`.

If an app or integration test needs simulated external provider behavior, prefer a WireMock-style test server or another explicit test harness over hidden in-process fake runtime wiring.

## Feature Placement

Organize code by feature first, then by role inside the feature.

Examples:

- `Features/Search/...`
- `Features/JustInTimeScheduling/...`
- `Features/BacklogScheduling/...`
- `Features/Albums/GetAlbum/...`

Avoid dumping unrelated types into broad utility folders.

Area folders may group related use-case features, but the concrete feature folder should still be the use case itself.

Examples:

- `Features/Artists/GetArtist/...`
- `Features/Artists/ListTracksByArtist/...`
- `Features/Albums/ListTracksByAlbum/...`

If a folder does not contain a handler or other entrypoint in its root, it is not a feature folder. It should be a descriptive subfolder inside a concrete feature folder.

When adding a new feature:

- put the endpoint adapter inside the owning API feature folder
- put the handler in the owning feature project, not in `Soundtrail.Domain`
- put the business models in `Soundtrail.Domain`
- put adapter implementations in the relevant infrastructure area
- mirror the feature structure in tests
- keep exactly one file in the concrete feature folder root: the handler or other entrypoint
- place handlers directly in the feature folder root, not in nested subfolders
- give each concrete feature its own `CompositionRoot` folder
- do not share a `CompositionRoot` folder between sibling features
- place `ServiceCollectionExtensions` and feature wiring options in that feature's own `CompositionRoot` folder
- place adapter implementations in an `Adapters` folder
- place supporting non-entrypoint files in a descriptive folder named for their role
- use folders such as `Model`, `Ports`, `Contracts`, `Policies`, or `Mapping` when they describe the support types clearly

Handler placement is a hard rule.

Examples:

- `Features/Search/SearchCatalog/SearchCatalogHandler.cs`
- `Features/Search/SearchCatalog/CompositionRoot/ServiceCollectionExtensions.cs`
- `Features/Search/SearchCatalog/Adapters/SearchCatalogEndpoints.cs`
- `Features/JustInTimeScheduling/LookupMusicRequestHandler.cs`
- `Features/Search/SearchCatalog/Ports/ICatalogSearchPort.cs`
- `Shared/ProviderContract.cs`

Only the single feature entrypoint belongs in the concrete feature folder root.

Allowed examples:

- handlers
- HTTP endpoints
- queue listeners
- health checks

Do not place handlers in nested folders such as:

- `Features/Search/Handlers/...`
- `Features/EnrichmentResponse/Application/...`

Do not place support files directly in a feature folder root, including:

- `ServiceCollectionExtensions`
- feature options
- helper mappers
- adapter implementations
- port interfaces
- requests and responses that are not entrypoints
- value objects and supporting model types
- DTOs
- indexes
- documents

For API features, HTTP endpoint classes are adapters. If a feature has both a handler and HTTP endpoint, keep the handler in the root and place the endpoint under `Adapters`.

## Endpoints

Endpoints should be thin.

Endpoints should:

- parse transport input
- convert input into domain request models
- return `400` for invalid user input
- delegate behavior to a handler
- translate handler output into HTTP contracts

Endpoints should not:

- contain branching business rules
- call infrastructure directly when a handler/port abstraction should own the behavior
- construct provider-specific logic on the request path

The same rule applies to other transport adapters such as Wolverine listeners, queue consumers, CDC subscribers, and projection triggers.

These adapters should stay thin.

They should:

- parse transport contracts
- convert contracts into domain commands or requests
- delegate to a handler
- translate handler output back into transport contracts when needed

They should not:

- reserve budgets
- mutate discovery lifecycle state directly
- contain retry, failure, or orchestration rules that belong in a handler

## Handlers

Handlers are the default place for use-case behavior.

Handlers should:

- model one business use case
- depend on ports and policies, not concrete adapters
- keep flow explicit and readable
- return domain/application results rather than HTTP-specific results
- live at the root of their feature folder
- live outside `Soundtrail.Domain`
- do one thing only
- be small enough that the top-level behavior is obvious without inlining helpers

Handlers should not:

- reach into configuration directly
- know about ASP.NET response details
- hide core decisions behind unnecessary abstraction
- be placed in nested subfolders under a feature
- live in `Soundtrail.Domain`
- write to two streams
- both append events and send commands in the same flow
- both project state and perform orchestration side effects in the same flow
- contain large multi-branch orchestration logic when that branching belongs to aggregate or domain consistency

Mandatory handler rule:

- a handler may append events to one stream, or send internal or integration commands, or project events into a read model or another stream
- a handler must not do more than one of those in the same flow
- if meaningful branching exists, that branching should usually be owned by an aggregate command instead of the handler

Mandatory race-avoidance rule:

- no handler should coordinate writes across catalog stream and discovery stream
- cross-stream consequences must be handled by projection or subscription, never by dual writes
- fan-out must be event-driven, replay-safe, and idempotent by identity
- commands emitted from projections must be idempotent by identity

## Value Types And Models

Prefer explicit value types for important inputs and identifiers.

Use value types for:

- ids
- search queries
- limits
- confidence scores
- artist/title fields
- other constrained business concepts

Value types should:

- validate at creation time
- expose a simple `From(...)` factory when that is the established pattern
- keep normalization and invariants close to the data they protect

Do not spread validation rules for the same concept across multiple layers.

## Domain Models And DTOs

Domain commands, responses, events, value types, and port contracts belong in a business-owned project.

Rules:

- domain objects are used by technology-independent handlers and business logic
- domain objects must not be designed around serialization concerns
- domain objects are never the place for HTTP, RavenDB, Service Bus, or other transport/storage DTO concerns
- technology-independent command base types, orchestration results, and port-facing read models belong in `Soundtrail.Domain`
- any DTO must end with `Dto`
- shared DTOs must live in `Soundtrail.Contracts` and end with `Dto`
- DTOs belong in API, messaging, persistence, or other infrastructure-owned areas
- ports must expose domain objects, never DTOs
- endpoints and adapters are responsible for mapping between DTOs and domain objects
- shared domain commands, responses, events, value types, and port contracts must live in `Soundtrail.Domain`
- persisted infrastructure DTOs should use a `RecordDto` suffix by default
- do not place app-local ports in `Soundtrail.Domain`; keep them in the owning app or feature project when only that app's handlers use them

If a type exists because JSON, RavenDB, or messaging needs a particular shape, it is a DTO and should not be passed through a business port.

## Ports And Adapters

Adapters are thin translation and wiring layers around real technology.

Rules:

- adapters should translate transport or infrastructure concerns into domain commands, domain events, or domain models, then delegate immediately
- adapters must not hide business rules that should be sociably unit tested through handlers
- infrastructure complexity such as retries, subscriptions, timers, serialization, and SDK concerns should stay in adapters, but business branching should not
- production applications must not rely on in-memory fakes to run locally; if a stubbed dependency is needed, use an explicit emulator or test server
- every adapter port must have integration tests that cover the fake and real implementation under the same test suite
- those integration tests are mandatory and must exercise every supported path for the port

Ports define what the business layer needs. Adapters satisfy those ports.

Guidelines:

- define ports in the business-owned project
- only define a port in `Soundtrail.Domain` when it is a true shared business contract used across app boundaries
- if a port is only used by handlers inside one app or one app-owned feature area, place it in that owning app project instead
- keep port interfaces small and use-case oriented
- keep adapter-specific mapping and SDK code in the API or worker infrastructure project
- place concrete adapters in an `Adapters` folder under the owning feature when they are feature-specific
- place adapter-owned DTOs, documents, indexes, and transport helpers under descriptive subfolders beneath `Adapters` when needed
- do not leak Raven or Azure SDK types through ports
- do not leak DTOs through ports

## Error Handling

Use exceptions sparingly and intentionally.

Prefer:

- validated value objects for invalid input
- explicit response/result models for expected business outcomes
- focused exceptions for exceptional scheduling/resolution failures

Do not use exceptions as normal control flow when a result model would make the behavior clearer.

## Naming

Follow the naming style already used in the repository.

- use clear, literal names
- name handlers as `<UseCase>Handler`
- name request and response models as `<UseCase>Request` and `<UseCase>Response`
- name interfaces by role, for example `ICatalogSearchPort`
- name test methods in `Given_When_Then` style
- keep the file path for handlers as `Features/<FeatureName>/<UseCase>Handler.cs`

Avoid vague names such as `Helper`, `Utils`, `Manager`, or `Processor` unless the type genuinely matches that abstraction.

## Code Style

- Prefer small files with a single clear purpose.
- Prefer straightforward control flow over indirection.
- Keep constructors simple and explicit.
- Use immutability by default where practical.
- Add comments only when they explain non-obvious intent.
- Do not introduce inheritance-heavy patterns for application logic or tests.

## Testing Standards

### Default Test Strategy

Follow outside-in TDD:

1. start with the behavior we want
2. write the failing test
3. implement the smallest clear change
4. refactor only after behavior is proven

### Unit Tests

Default to sociable unit tests.

That means:

- exercise a handler, scheduler, or other meaningful unit with its real in-memory collaborators
- use custom fakes for ports
- let tests cover policies through the public behavior of the unit when practical

Avoid solitary tests for small internal policies when the same rule can be proven through a more meaningful unit-level scenario.

Good unit-test targets:

- handlers
- schedulers
- domain models with invariants
- request/response decision points
- orchestration flows that react to adapter failures through ports

Unit tests should not target transport adapters such as listeners, endpoints, hosted services, CDC subscribers, or projection subscription services.

Those adapters belong in integration coverage.

Additional unit-test rules:

- place unit tests under a `Unit` folder
- name unit test files by business scenario, for example `AlbumExistsTests.cs`
- do not use `GivenWhenThen` as a test file name
- keep test class names scenario-focused and business-readable
- keep test method names business-driven and avoid technical concepts such as handler, endpoint, controller, listener, or adapter unless the technical boundary is the actual business subject under test
- do not add comments such as `Given`, `When`, or `Then` inside the test body
- each test should validate one observable property or business outcome
- each test should usually specify only the one field relevant to that test and rely on defaults for the rest
- prefer helper or data-factory methods with optional parameters and sensible defaults so irrelevant setup stays implicit

### Solitary Tests

Solitary tests are acceptable when a type has meaningful standalone behavior that would become awkward or noisy to prove indirectly.

Examples:

- constrained value objects
- pure transformation rules with no richer unit around them

Do not default to policy-only tests when the surrounding handler or scheduler can prove the same rule clearly.

### Fakes

Use custom fakes instead of mocking frameworks by default.

Fakes should:

- be simple to read
- capture the behavior needed by the test
- expose recorded interactions only where that interaction is part of the business outcome
- follow the name pattern `<Role>Fake`, for example `CommandBusFake` for `ICommandBus`

Do not add a mocking library for routine application tests.

### Contract Tests

Where a fake represents an infrastructure adapter, add contract tests that run against:

- the fake
- the real adapter

Contract tests should prove equivalent observable behavior, not internal implementation details.

These adapter contract tests are mandatory.

Rules:

- every adapter port with a fake must have integration coverage
- the fake and real implementation must be exercised by the same test cases
- prefer `Theory` or equivalent shared test fixtures so the assertions stay identical across modes
- do not write one test class for the fake and a separate loosely-mirrored class for the real adapter when a shared contract test is possible
- a fake is not complete until the shared fake/real contract test exists

### Test Organization

Mirror the production feature layout in tests.

Examples:

- `Unit/GetAlbum/...`
- `Unit/SearchCatalog/...`
- `Integration/GetAlbum/...`
- `Integration/Ports/CatalogSearch/...`

Prefer scenario-focused test classes over giant omnibus fixtures.

Integration tests should be grouped by the technology being exercised.

Examples:

- `Integration/Api/WebApi/...`
- `Integration/Api/Ports/Raven/...`
- `Integration/Enrichment/Ports/Http/...`

Do not use feature-level integration suites to prove broad business behavior when the real subject under test is a technology boundary such as Web API routing, RavenDB persistence, HTTP clients, or queue wiring.

### Web API Integration Tests

Web API route integration tests must stay intentionally minimal.

Rules:

- write one `200` status routing test per route
- add one error-model mapping test only when the route has a non-trivial error contract worth proving
- do not use Web API route tests to prove business rules, response-shaping branches, or adapter semantics that belong in handler, port, or technology-specific tests
- keep deeper response and storage behavior in unit tests, port contract tests, or technology-focused integration tests
- when response mapping is tested at the HTTP layer, keep each test focused on one returned field or one observable HTTP concern

### Integration Test Environments

Integration tests should use a test environment class with a static factory method.

Rules:

- encapsulate `WebApplicationFactory` inside the test environment class instead of exposing the factory directly to test classes
- prefer `Create(...)` or `CreateAsync(...)` as the entry point for building an environment
- let the environment own setup, seeding, HTTP client creation, and disposal
- keep tests focused on scenario intent rather than host wiring
- place the test environment in a separate file rather than inside the test class file
- keep the test environment constructor private
- expose scenario-based static factory methods so there is one standard way to create each business setup

### What New Feature Work Must Test

At minimum, new feature work should cover:

- happy path behavior
- important boundary values
- invalid input handling
- the branch where work is deferred or rejected
- any queueing, persistence, or deduplication behavior that is part of the business outcome

If a fake adapter is introduced, plan the contract test in the same change or immediately after.

## Public Request Path Rule

The public API path must not directly call third-party providers.

Public requests may:

- read local data
- read local caches
- create demand signals

Provider enrichment must happen asynchronously and under explicit worker control.

## New Feature Checklist

Before finishing a feature, confirm:

- the code sits in the correct project and feature folder
- business rules live in handlers/domain code, not endpoints
- ports do not leak infrastructure types
- tests are sociable by default
- fake adapters have or will have contract coverage
- the request path remains local-only where required
- the solution builds and relevant tests pass

## Default Verification Commands

Use the smallest command that proves the change.

Common examples:

- `dotnet test tests/Soundtrail.Services.Tests/Soundtrail.Services.Tests.csproj --filter FullyQualifiedName~Unit`
- `dotnet test`

Run the focused unit slice during iteration, then broaden verification when the change touches integration or infrastructure behavior.

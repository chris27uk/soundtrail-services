# Coding Standards

This document defines the default standards for new feature work in this solution.

The goal is consistency more than cleverness. New code should fit the existing shape of the solution, keep the business rules easy to read, and make tests prove behavior rather than implementation details.

## Primary Principles

- Keep the public API request path local, fast, and predictable.
- Put business decisions in application/domain code, not in endpoints or infrastructure adapters.
- Prefer small, explicit types over primitive-heavy code.
- Follow outside-in TDD.
- Use custom fakes and sociable unit tests by default.
- Add integration tests only where they prove a fake matches a real adapter or where infrastructure behavior matters.

## Solution Shape

The active projects in this solution are:

- `src/Soundtrail.Services`
- `src/Soundtrail.Services.Api`
- `src/Soundtrail.Services.Enrichment`
- `src/Soundtrail.Services.Enrichment.Scheduler`

Treat these as the authoritative structure for new work unless the solution is deliberately reorganized.

## Layer Responsibilities

### `Soundtrail.Services`

Use this project for core application and domain behavior that is shared by the API path.

Allowed here:

- handlers
- request/response models
- domain value types
- core business rules
- small shared abstractions such as ports and ids

Not allowed here:

- ASP.NET types
- Raven, Azure Service Bus, or other infrastructure SDK types
- configuration binding
- transport concerns

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

- `Api` may depend on `Services`
- `Enrichment.Scheduler` may depend on `Enrichment` and shared core code
- `Enrichment` may depend on `Services` where shared message/value types are needed
- core business projects must not depend on API or infrastructure projects

If a type only exists to satisfy HTTP, Raven, Azure Service Bus, or host startup, it should not live in a core business project.

## Feature Placement

Organize code by feature first, then by role inside the feature.

Examples:

- `Features/Search/...`
- `Features/JustInTimeScheduling/...`
- `Features/BacklogScheduling/...`

Avoid dumping unrelated types into broad utility folders.

When adding a new feature:

- put the endpoint in the API feature folder
- put the handler and business models in the core project
- put adapter implementations in the relevant infrastructure area
- mirror the feature structure in tests

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

## Handlers

Handlers are the default place for use-case behavior.

Handlers should:

- model one business use case
- depend on ports and policies, not concrete adapters
- keep flow explicit and readable
- return domain/application results rather than HTTP-specific results

Handlers should not:

- reach into configuration directly
- know about ASP.NET response details
- hide core decisions behind unnecessary abstraction

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

## Ports And Adapters

Ports define what the business layer needs. Adapters satisfy those ports.

Guidelines:

- define ports in the business-owned project
- keep port interfaces small and use-case oriented
- keep adapter-specific mapping and SDK code in the API or worker infrastructure project
- do not leak Raven or Azure SDK types through ports

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
- name interfaces by role, for example `ITrackSearchPort`
- name tests in `Given_When_Then` style

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

Do not add a mocking library for routine application tests.

### Contract Tests

Where a fake represents an infrastructure adapter, add contract tests that run against:

- the fake
- the real adapter

Contract tests should prove equivalent observable behavior, not internal implementation details.

### Test Organization

Mirror the production feature layout in tests.

Examples:

- `Api/Unit/Features/Search/...`
- `Enrichment/Unit/Features/Scheduling/...`
- `Api/Integration/Ports/TrackSearch/...`

Prefer scenario-focused test classes over giant omnibus fixtures.

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

using Xunit;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

/// <summary>
/// Groups messaging activity-listener tests. Isolation is via <see cref="ActivityProbe"/>'s
/// per-test root activity (RootId filter), not assembly-wide DisableParallelization.
/// </summary>
[CollectionDefinition(nameof(ActivityTelemetryCollection))]
public sealed class ActivityTelemetryCollection;

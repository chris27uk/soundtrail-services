using Xunit;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

/// <summary>
/// ActivityListener is process-global; serialize probes so parallel collections cannot steal stops.
/// </summary>
[CollectionDefinition(nameof(ActivityTelemetryCollection), DisableParallelization = true)]
public sealed class ActivityTelemetryCollection;

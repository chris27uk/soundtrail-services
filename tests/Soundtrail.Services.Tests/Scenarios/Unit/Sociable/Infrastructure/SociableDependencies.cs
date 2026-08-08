using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

internal sealed class SociableDependencies
{
    public DateTimeOffset UtcNow { get; init; }

    public CommandBusFake CommandBus { get; init; } = CommandBusFake.Empty();

    /// <summary>
    /// Replaces the default adapter of the same concrete type after discovery.
    /// </summary>
    public IReadOnlyList<IFeature> ReplaceAdapters { get; init; } = [];
}

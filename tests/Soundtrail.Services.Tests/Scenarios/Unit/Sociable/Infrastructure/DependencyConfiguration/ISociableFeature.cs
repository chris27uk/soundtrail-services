using Soundtrail.Adapters.FeatureOrchestration;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;

/// <summary>
/// Marker for sociable test adapters discovered by <see cref="SociableDiscoveryEngine"/>.
/// </summary>
internal interface ISociableFeature : IFeature;

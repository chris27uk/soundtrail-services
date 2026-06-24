namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public sealed record CatalogProviderReferenceProjection(
    string Provider,
    string ProviderEntityType,
    string ProviderId,
    string Url,
    DateTimeOffset DiscoveredAt);

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

public sealed record CatalogProviderReferenceProjection(
    string Provider,
    string ProviderEntityType,
    string ProviderId,
    string Url,
    DateTimeOffset DiscoveredAt);

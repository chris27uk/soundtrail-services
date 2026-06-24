using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.IntegrationEvents;

public abstract record MusicTrackIntegrationEvent(
    MusicCatalogId MusicCatalogId);

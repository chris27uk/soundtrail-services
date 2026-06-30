using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.Enrichment;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.MusicTrackEventStore;
using Soundtrail.Contracts.Common;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Support;

internal static class TestEventStreamRepositories
{
    public static IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> CreateDiscoveryQuery(IAsyncDocumentSession session) =>
        new RavenEventStreamRepository<DiscoveryQueryKey, IDomainEvent>(
            session,
            TypeTranslationRegistry.Default,
            DiscoveryQueryEventStreamDefinition.Create());

    public static IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> CreateMusicTrack(IAsyncDocumentSession session) =>
        new RavenEventStreamRepository<MusicCatalogId, IMusicTrackEvent>(
            session,
            TypeTranslationRegistry.Default,
            MusicTrackEventStreamDefinition.Create());

    public static IEventStreamRepository<ArtistId, IDomainEvent> CreateArtistCatalog(IAsyncDocumentSession session) =>
        new RavenEventStreamRepository<ArtistId, IDomainEvent>(
            session,
            TypeTranslationRegistry.Default,
            ArtistCatalogEventStreamDefinition.Create());

    public static IEventStreamRepository<MusicCatalogLookupId, IDomainEvent> CreateMusicCatalogLookup(IAsyncDocumentSession session) =>
        new RavenEventStreamRepository<MusicCatalogLookupId, IDomainEvent>(
            session,
            TypeTranslationRegistry.Default,
            MusicCatalogLookupEventStreamDefinition.Create());
}

using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnDiscoveryRequested;
using Soundtrail.Services.Internal.Projector.Features.OnDiscoveryRequested.Support;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

internal sealed class OnDiscoveryRequestedHandlerTestEnvironment
{
    private static readonly MusicSearchCriteria SearchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
    private static readonly DateTimeOffset RequestedAt = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

    private OnDiscoveryRequestedHandlerTestEnvironment()
    {
        Bus = new CommandBusFake();
        Handler = new OnDiscoveryRequestedHandler(Bus);
    }

    public CommandBusFake Bus { get; }

    public OnDiscoveryRequestedHandler Handler { get; }

    public static OnDiscoveryRequestedHandlerTestEnvironment Create() => new();

    public DiscoveryRequestedCommand CommandWithPlayback() =>
        new(
            DiscoveryQueryKey.For(SearchCriteria),
            [new VersionedCatalogSearchDiscoveryEvent(
                1,
                new DiscoveryRequested(
                    SearchCriteria,
                    PlaybackProviderFilter.Parse("spotify,appleMusic"),
                    1,
                    10,
                    RequestedAt,
                    CorrelationId.From("corr-1")))]);

    public DiscoveryRequestedCommand CommandWithoutPlayback() =>
        new(
            DiscoveryQueryKey.For(SearchCriteria),
            [new VersionedCatalogSearchDiscoveryEvent(
                1,
                new DiscoveryRequested(
                    SearchCriteria,
                    null,
                    1,
                    10,
                    RequestedAt,
                    CorrelationId.From("corr-1")))]);
}

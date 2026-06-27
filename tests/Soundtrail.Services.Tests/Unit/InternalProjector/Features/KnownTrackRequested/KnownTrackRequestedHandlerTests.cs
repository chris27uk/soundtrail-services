using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Features.KnownTrackRequested;

public sealed class KnownTrackRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Known_Track_Request_With_Missing_Providers_When_Handled_Then_Streaming_Locations_Required_Is_Appended()
    {
        var env = KnownTrackRequestedHandlerTestEnvironment.Create();
        env.TrackRequiresStreamingLocations();
        await env.SeedKnownTrackRequestedAsync();

        await env.Handler.Handle(env.Command(), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForTrack(TrackId.From("track_1")))
            .Should()
            .Contain(x => x is StreamingLocationsRequired);
    }

    [Fact]
    public async Task Given_A_Missing_Track_When_Handled_Then_No_Follow_Up_Event_Is_Appended()
    {
        var env = KnownTrackRequestedHandlerTestEnvironment.Create();
        env.TrackIsMissing();
        await env.SeedKnownTrackRequestedAsync();

        await env.Handler.Handle(env.Command(), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForTrack(TrackId.From("track_1")))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<KnownTrackRequestedEvent>();
    }
}

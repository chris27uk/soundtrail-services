using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Features.KnownTrackRequested;

public sealed class KnownTrackRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Known_Track_Request_With_Missing_Providers_When_Handled_Then_A_Streaming_Locations_Lookup_Is_Dispatched()
    {
        var env = KnownTrackRequestedHandlerTestEnvironment.Create();
        env.TrackRequiresStreamingLocations();

        await env.Handler.Handle(env.Command(), CancellationToken.None);

        env.Bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<LookupStreamingLocationsCommand>()
            .Which.Should().BeEquivalentTo(new
            {
                MusicCatalogId = Soundtrail.Contracts.Common.MusicCatalogId.From("track_1"),
                Priority = Soundtrail.Contracts.Common.LookupPriorityBand.Low,
                LookupKey = Soundtrail.Domain.Search.MusicSearchCriteria.ByIsrc("isrc-1")
            });
    }

    [Fact]
    public async Task Given_A_Missing_Track_When_Handled_Then_No_Lookup_Command_Is_Dispatched()
    {
        var env = KnownTrackRequestedHandlerTestEnvironment.Create();
        env.TrackIsMissing();

        await env.Handler.Handle(env.Command(), CancellationToken.None);

        env.Bus.SentCommands.Should().BeEmpty();
    }
}

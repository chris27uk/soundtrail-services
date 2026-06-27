using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class StreamingLocationsRequiredListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_StreamingLocationsRequired_Message_When_Handled_Then_A_StreamingLocations_Command_Dto_Is_Sent()
    {
        var bus = new WolverineMessageBusFake();
        var listener = new StreamingLocationsRequiredListener(new StreamingLocationsRequiredHandler(new WolverineCommandBus(bus)));
        await listener.Handle(
            new StreamingLocationsRequiredMessageDto(
                "mc_track_1",
                LookupPriorityBand.High,
                "corr-1",
                ProviderName.MusicBrainz.Value,
                new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
                new StreamingLocationSearchTermDto(MusicSearchKind.Isrc, null, "isrc-1", null, null, null),
                "artist_test_artist",
                "album_rare_album"),
            null!);

        var dto = bus.SentMessages.Single().Should().BeOfType<LookupStreamingLocationsCommandDto>().Subject;
        dto.CommandId.Should().Be(CommandId.For("LookupStreamingLocations:mc_track_1").Value);
        dto.MusicCatalogId.Should().Be("mc_track_1");
        dto.SearchTerm.Kind.Should().Be(MusicSearchKind.Isrc);
        dto.SearchTerm.Isrc.Should().Be("isrc-1");
        dto.ArtistId.Should().Be("artist_test_artist");
        dto.AlbumId.Should().Be("album_rare_album");
    }
}

using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup;
using Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class MusicTrackEventListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_PlaybackReferencesResolutionRequired_Message_When_Handled_Then_A_PlaybackReferences_Command_Dto_Is_Sent()
    {
        var bus = new WolverineMessageBusFake();
        var listener = new MusicTrackEventListener(new SchedulePlaybackReferencesLookupHandler(new WolverineCommandBus(bus)));
        await listener.Handle(
            new PlaybackReferencesResolutionRequiredMessageDto(
                "mc_track_1",
                LookupPriorityBand.High,
                "corr-1",
                ProviderName.MusicBrainz.Value,
                new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
                new PlaybackReferenceSearchTermDto("isrc-1", null, null, null),
                "artist_test_artist",
                "album_rare_album"),
            null!);

        var dto = bus.SentMessages.Single().Should().BeOfType<ResolvePlaybackReferencesCommandDto>().Subject;
        dto.CommandId.Should().Be(CommandId.For("ResolvePlaybackReferences:mc_track_1").Value);
        dto.MusicCatalogId.Should().Be("mc_track_1");
        dto.SearchTerm.Isrc.Should().Be("isrc-1");
        dto.ArtistId.Should().Be("artist_test_artist");
        dto.AlbumId.Should().Be("album_rare_album");
    }
}

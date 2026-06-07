using FluentAssertions;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Orchestration;

public sealed class MusicTrackBusinessIntentListenerTests
{
    [Fact]
    public void Given_AppleMusicResolutionRequired_When_Handled_Then_An_AppleResolutionCommand_Is_Produced()
    {
        var listener = new MusicTrackBusinessIntentListener();

        var message = listener.Handle(
            new AppleMusicResolutionRequired(
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                CorrelationId.From("corr-1"),
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)));

        message.Should().BeOfType<HighPriorityResolveApplePlaybackReferenceCommandMessage>();
        var typed = (HighPriorityResolveApplePlaybackReferenceCommandMessage)message;
        typed.Command.CommandId.Should().Be(CommandId.For("ResolveApplePlaybackReference:mc_track_1"));
        typed.Command.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public void Given_YouTubeMusicResolutionRequired_When_Handled_Then_A_YouTubeResolutionCommand_Is_Produced()
    {
        var listener = new MusicTrackBusinessIntentListener();

        var message = listener.Handle(
            new YouTubeMusicResolutionRequired(
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.Low,
                CorrelationId.From("corr-2"),
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 6, 12, 5, 0, TimeSpan.Zero)));

        message.Should().BeOfType<LowPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage>();
        var typed = (LowPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage)message;
        typed.Command.CommandId.Should().Be(CommandId.For("ResolveYouTubeMusicPlaybackReference:mc_track_1"));
        typed.Command.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }
}

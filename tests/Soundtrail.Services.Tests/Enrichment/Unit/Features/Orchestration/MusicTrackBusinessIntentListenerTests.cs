using FluentAssertions;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Orchestration;

public sealed class MusicTrackEventCommandHandlerTests
{
    [Fact]
    public void Given_AppleMusicResolutionRequired_When_Handled_Then_An_AppleResolutionCommand_Is_Produced()
    {
        var listener = new MusicTrackEventCommandHandler();

        var message = listener.Handle(
            new AppleMusicResolutionRequired(
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                CorrelationId.From("corr-1"),
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)));

        message.Should().BeOfType<ResolveApplePlaybackReferenceCommand>();
        var typed = (ResolveApplePlaybackReferenceCommand)message;
        typed.CommandId.Should().Be(CommandId.For("ResolveApplePlaybackReference:mc_track_1"));
        typed.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public void Given_YouTubeMusicResolutionRequired_When_Handled_Then_A_YouTubeResolutionCommand_Is_Produced()
    {
        var listener = new MusicTrackEventCommandHandler();

        var message = listener.Handle(
            new YouTubeMusicResolutionRequired(
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.Low,
                CorrelationId.From("corr-2"),
                ProviderName.MusicBrainz,
                new DateTimeOffset(2026, 6, 6, 12, 5, 0, TimeSpan.Zero)));

        message.Should().BeOfType<ResolveYouTubeMusicPlaybackReferenceCommand>();
        var typed = (ResolveYouTubeMusicPlaybackReferenceCommand)message;
        typed.CommandId.Should().Be(CommandId.For("ResolveYouTubeMusicPlaybackReference:mc_track_1"));
        typed.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }
}

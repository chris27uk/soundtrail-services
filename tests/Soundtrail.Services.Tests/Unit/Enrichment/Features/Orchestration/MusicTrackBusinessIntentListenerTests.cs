using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Orchestration;

public sealed class MusicTrackEventCommandHandlerTests
{
    [Fact]
    public void Given_AppleMusicResolutionRequired_When_Handled_Then_An_AppleResolutionCommand_Is_Produced()
    {
        var message = new MusicTrackEventCommandHandler().Handle(AppleResolutionRequired());

        message.Should().BeOfType<ResolveApplePlaybackReferenceCommand>();
    }

    [Fact]
    public void Given_AppleMusicResolutionRequired_When_Handled_Then_The_Apple_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var message = (ResolveApplePlaybackReferenceCommand)new MusicTrackEventCommandHandler().Handle(AppleResolutionRequired());

        message.CommandId.Should().Be(CommandId.For("ResolveApplePlaybackReference:mc_track_1"));
    }

    [Fact]
    public void Given_AppleMusicResolutionRequired_When_Handled_Then_The_MusicCatalogId_Is_Preserved_On_The_Apple_Command()
    {
        var message = (ResolveApplePlaybackReferenceCommand)new MusicTrackEventCommandHandler().Handle(AppleResolutionRequired());

        message.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public void Given_YouTubeMusicResolutionRequired_When_Handled_Then_A_YouTubeResolutionCommand_Is_Produced()
    {
        var message = new MusicTrackEventCommandHandler().Handle(YouTubeResolutionRequired());

        message.Should().BeOfType<ResolveYouTubeMusicPlaybackReferenceCommand>();
    }

    [Fact]
    public void Given_YouTubeMusicResolutionRequired_When_Handled_Then_The_YouTube_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var message = (ResolveYouTubeMusicPlaybackReferenceCommand)new MusicTrackEventCommandHandler().Handle(YouTubeResolutionRequired());

        message.CommandId.Should().Be(CommandId.For("ResolveYouTubeMusicPlaybackReference:mc_track_1"));
    }

    [Fact]
    public void Given_YouTubeMusicResolutionRequired_When_Handled_Then_The_MusicCatalogId_Is_Preserved_On_The_YouTube_Command()
    {
        var message = (ResolveYouTubeMusicPlaybackReferenceCommand)new MusicTrackEventCommandHandler().Handle(YouTubeResolutionRequired());

        message.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    private static AppleMusicResolutionRequired AppleResolutionRequired() =>
        new(
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            CorrelationId.From("corr-1"),
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero));

    private static YouTubeMusicResolutionRequired YouTubeResolutionRequired() =>
        new(
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.Low,
            CorrelationId.From("corr-2"),
            ProviderName.MusicBrainz,
            new DateTimeOffset(2026, 6, 6, 12, 5, 0, TimeSpan.Zero));
}

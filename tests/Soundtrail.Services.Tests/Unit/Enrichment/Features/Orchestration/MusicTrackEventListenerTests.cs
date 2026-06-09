using FluentAssertions;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Orchestration;

public sealed class MusicTrackEventListenerTests
{
    [Fact]
    public void Given_A_PlaybackReferencesResolutionRequired_Message_When_Handled_Then_A_PlaybackReferences_Command_Dto_Is_Returned()
    {
        var env = MusicTrackEventListenerTestEnvironment.WithPlaybackReferencesResolutionRequiredMessage();
        var message = env.HandlePlaybackReferencesResolutionRequired();
        message.Should().BeOfType<ResolvePlaybackReferencesCommandDto>();
    }

    [Fact]
    public void Given_A_PlaybackReferencesResolutionRequired_Message_When_Handled_Then_The_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var env = MusicTrackEventListenerTestEnvironment.WithPlaybackReferencesResolutionRequiredMessage();
        var message = (ResolvePlaybackReferencesCommandDto)env.HandlePlaybackReferencesResolutionRequired();
        message.CommandId.Should().Be(CommandId.For("ResolvePlaybackReferences:mc_track_1").Value);
    }

    [Fact]
    public void Given_A_PlaybackReferencesResolutionRequired_Message_When_Handled_Then_The_MusicCatalogId_Is_Preserved()
    {
        var env = MusicTrackEventListenerTestEnvironment.WithPlaybackReferencesResolutionRequiredMessage();
        var message = (ResolvePlaybackReferencesCommandDto)env.HandlePlaybackReferencesResolutionRequired();
        message.MusicCatalogId.Should().Be("mc_track_1");
        message.LookupKey.Mode.Should().Be(PlaybackReferenceLookupModeDto.Isrc);
    }
}

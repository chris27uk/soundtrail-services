using FluentAssertions;
using Soundtrail.Contracts.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class LookupExecutionCommandMessageExtensionsTests
{
    [Fact]
    public void Given_A_High_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var message = HighPriorityCommand().ToDto();

        message.CommandId.Should().Be(CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value);
    }

    [Fact]
    public void Given_A_High_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_MusicCatalogId_Is_Preserved()
    {
        var message = HighPriorityCommand().ToDto();

        message.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact]
    public void Given_A_High_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_Priority_Is_Preserved()
    {
        var message = HighPriorityCommand().ToDto();

        message.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public void Given_A_Low_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var message = LowPriorityCommand().ToDto();

        message.CommandId.Should().Be(CommandId.For("ResolveCanonicalMetadata:mc_track_2").Value);
    }

    [Fact]
    public void Given_A_Low_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_MusicCatalogId_Is_Preserved()
    {
        var message = LowPriorityCommand().ToDto();

        message.MusicCatalogId.Should().Be("mc_track_2");
    }

    [Fact]
    public void Given_A_Low_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_Priority_Is_Preserved()
    {
        var message = LowPriorityCommand().ToDto();

        message.Priority.Should().Be(LookupPriorityBand.Low);
    }

    private static LookupMusicCommand HighPriorityCommand() =>
        new(
            CommandId.For("mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-1"));

    private static LookupMusicCommand LowPriorityCommand() =>
        new(
            CommandId.For("mc_track_2"),
            MusicCatalogId.From("mc_track_2"),
            LookupPriorityBand.Low,
            new DateTimeOffset(2026, 6, 5, 12, 5, 0, TimeSpan.Zero),
            CorrelationId.From("corr-2"));
}

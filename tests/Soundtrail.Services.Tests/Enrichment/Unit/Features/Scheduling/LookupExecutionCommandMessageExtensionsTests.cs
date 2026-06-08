using FluentAssertions;
using Soundtrail.Contracts.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling;

public sealed class LookupExecutionCommandMessageExtensionsTests
{
    [Fact]
    public void Given_A_High_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_The_Command_Is_Provider_And_Priority_Specific()
    {
        var command = new LookupMusicCommand(
            CommandId.For("mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-1"));

        var message = command.ToDto();

        message.CommandId.Should().Be(CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value);
        message.MusicCatalogId.Should().Be("mc_track_1");
        message.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public void Given_A_Low_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Command_Then_The_Command_Is_Provider_And_Priority_Specific()
    {
        var command = new LookupMusicCommand(
            CommandId.For("mc_track_2"),
            MusicCatalogId.From("mc_track_2"),
            LookupPriorityBand.Low,
            new DateTimeOffset(2026, 6, 5, 12, 5, 0, TimeSpan.Zero),
            CorrelationId.From("corr-2"));

        var message = command.ToDto();

        message.CommandId.Should().Be(CommandId.For("ResolveCanonicalMetadata:mc_track_2").Value);
        message.MusicCatalogId.Should().Be("mc_track_2");
        message.Priority.Should().Be(LookupPriorityBand.Low);
    }
}

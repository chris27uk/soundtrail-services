using FluentAssertions;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Queuing;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling;

public sealed class LookupExecutionCommandMessageExtensionsTests
{
    [Fact]
    public void Given_A_High_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Message_Then_The_Command_Is_Provider_And_Priority_Specific()
    {
        var command = new LookupMusicCommand(
            CommandId.For("mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-1"));

        var message = command.ToResolveCanonicalMetadataTransportMessage();

        message.Should().BeOfType<HighPriorityResolveCanonicalMetadataCommandMessage>();

        var typedMessage = (HighPriorityResolveCanonicalMetadataCommandMessage)message;
        typedMessage.Command.TargetProvider.Should().Be(ProviderName.MusicBrainz);
        typedMessage.Command.CommandId.Should().Be(CommandId.For("ResolveCanonicalMetadata:mc_track_1"));
        typedMessage.Command.MusicCatalogId.Value.Should().Be("mc_track_1");
    }

    [Fact]
    public void Given_A_Low_Priority_Scheduled_Lookup_Command_When_Building_A_MusicBrainz_Message_Then_The_Command_Is_Provider_And_Priority_Specific()
    {
        var command = new LookupMusicCommand(
            CommandId.For("mc_track_2"),
            MusicCatalogId.From("mc_track_2"),
            LookupPriorityBand.Low,
            new DateTimeOffset(2026, 6, 5, 12, 5, 0, TimeSpan.Zero),
            CorrelationId.From("corr-2"));

        var message = command.ToResolveCanonicalMetadataTransportMessage();

        message.Should().BeOfType<LowPriorityResolveCanonicalMetadataCommandMessage>();

        var typedMessage = (LowPriorityResolveCanonicalMetadataCommandMessage)message;
        typedMessage.Command.TargetProvider.Should().Be(ProviderName.MusicBrainz);
        typedMessage.Command.CommandId.Should().Be(CommandId.For("ResolveCanonicalMetadata:mc_track_2"));
        typedMessage.Command.MusicCatalogId.Value.Should().Be("mc_track_2");
    }
}

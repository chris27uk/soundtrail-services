using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.ImportKworbChart.Scenarios.NoExistingDataOrRequests;

public sealed class ImportKworbChartTriggerTests
{
    [Fact]
    public async Task When_Triggered_Then_The_Request_Targets_The_Worldwide_Song_Chart_Playlist()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        environment.SentMessage<RequestKnownMusicDataMessage>().Operation.Should().Be(
            new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId.FromPlaylistName("WorldwideSongChart")));
    }

    [Fact]
    public async Task When_Triggered_Then_The_Request_Uses_High_Priority()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        environment.SentMessage<RequestKnownMusicDataMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task When_Triggered_Then_The_Trigger_Window_Is_Aligned_To_The_Hour()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();
        var triggeredAt = new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero);

        await environment.TriggerImportAsync(triggeredAt);

        environment.SentMessage<RequestKnownMusicDataMessage>().RequestedAt.Should().Be(
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task When_Triggered_Then_The_Command_Id_Is_Deterministic_Per_Hour()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();
        var triggeredAt = new DateTimeOffset(2026, 7, 19, 10, 59, 59, TimeSpan.Zero);

        await environment.TriggerImportAsync(triggeredAt);

        environment.SentMessage<RequestKnownMusicDataMessage>().Id.Value.Should().Be(
            "kworb:worldwidesongchart:2026071910");
    }

    [Fact]
    public async Task When_Triggered_Then_The_Correlation_Id_Matches_The_Command_Scope()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId.Value.Should().Be(
            "kworb:worldwidesongchart:2026071910");
    }

    [Fact]
    public async Task When_Triggered_Twice_In_The_Same_Hour_Then_They_Produce_The_Same_Command_Scope()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 1, 0, TimeSpan.Zero));
        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 59, 0, TimeSpan.Zero));

        environment.SentMessages<RequestKnownMusicDataMessage>()
            .Select(x => x.Id.Value)
            .Should()
            .OnlyContain(x => x == "kworb:worldwidesongchart:2026071910");
    }

    [Fact]
    public async Task When_Triggered_In_Different_Hours_Then_They_Produce_Different_Command_Scopes()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 59, 0, TimeSpan.Zero));
        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 11, 0, 0, TimeSpan.Zero));

        environment.SentMessages<RequestKnownMusicDataMessage>()
            .Select(x => x.Id.Value)
            .Should()
            .Equal("kworb:worldwidesongchart:2026071910", "kworb:worldwidesongchart:2026071911");
    }

    [Fact]
    public async Task When_Triggered_Then_Work_Requested_Is_Saved()
    {
        var environment = ImportKworbChartSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.TriggerImportAsync(new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero));

        environment.SavedEvents<WorkRequested>()
            .Should()
            .ContainSingle(@event =>
                @event.Target.NormalisedIdentifier == $"child_tracks_for_playlist:{environment.PlaylistId.Value}");
    }
}

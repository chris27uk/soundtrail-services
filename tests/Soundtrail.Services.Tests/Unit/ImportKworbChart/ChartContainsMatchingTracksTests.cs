using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

public sealed class ChartContainsMatchingTracksTests
{
    [Fact]
    public async Task Given_A_Kworb_Trigger_When_Handling_It_Then_A_Known_Playlist_Request_Is_Sent()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Kworb_Trigger_When_Handling_It_Then_The_Request_Targets_The_Worldwide_Song_Chart_Playlist()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Operation.Should().Be(
            new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId.FromPlaylistName("WorldwideSongChart")));
    }

    [Fact]
    public async Task Given_A_Kworb_Trigger_When_Handling_It_Then_The_Request_Uses_High_Priority()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_A_Kworb_Trigger_When_Handling_It_Then_The_Trigger_Window_Is_Aligned_To_The_Hour()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();
        var triggeredAt = new DateTimeOffset(2026, 7, 19, 10, 23, 45, TimeSpan.Zero);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest(triggeredAt));

        environment.CommandBus.Commands.Single().RequestedAt.Should().Be(new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Given_A_Kworb_Trigger_When_Handling_It_Then_The_Command_Id_Is_Deterministic_Per_Hour()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();
        var triggeredAt = new DateTimeOffset(2026, 7, 19, 10, 59, 59, TimeSpan.Zero);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest(triggeredAt));

        environment.CommandBus.Commands.Single().Id.Value.Should().Be("kworb:worldwidesongchart:2026071910");
    }

    [Fact]
    public async Task Given_A_Kworb_Trigger_When_Handling_It_Then_The_Correlation_Id_Matches_The_Command_Scope()
    {
        var environment = ImportKworbChartUnitTestEnvironment.Create();

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.CommandBus.Commands.Single().CorrelationId.Value.Should().Be("kworb:worldwidesongchart:2026071910");
    }
}

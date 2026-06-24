using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class LookupStreamingLocationsListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Successful_Playback_Reference_Lookup_When_Handled_Then_An_Enrichment_Response_Message_Is_Returned()
    {
        var env = LookupStreamingLocationsHandlerTestEnvironment.Create();
        env.Seed(
            MusicSearchTerm.ByIsrc("isrc-1"),
            new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/us/song/apple-1?i=apple-1"), "apple-1"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupStreamingLocationsListener(env.Handler, bus);

        await listener.Handle(Command(), null!);

        bus.SentMessages.Should().ContainSingle().Which.Should().BeOfType<MusicCatalogLookupAttemptedDto>();
    }

    [Fact]
    public async Task Given_A_Budget_Deferred_Playback_Reference_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupStreamingLocationsHandlerTestEnvironment.Create();
        env.SourceBudget.Reject(
            ProviderName.Odesli,
            new DateTimeOffset(2026, 6, 18, 12, 1, 0, TimeSpan.Zero),
            "Odesli budget temporarily unavailable");
        var bus = new WolverineMessageBusFake();
        var listener = new LookupStreamingLocationsListener(env.Handler, bus);

        await listener.Handle(Command(), null!);
        var message = bus.SentMessages.Single().Should().BeOfType<MusicCatalogLookupAttemptedDto>().Subject;

        message.Outcome.Status.Should().Be("Deferred");
        message.SourceProvider.Should().Be(ProviderName.Odesli.Value);
        message.Outcome.Reason.Should().Be("Odesli budget temporarily unavailable");
    }

    [Fact]
    public async Task Given_A_Failed_Playback_Reference_Lookup_When_Handled_Then_A_Lookup_Execution_Report_Is_Returned()
    {
        var env = LookupStreamingLocationsHandlerTestEnvironment.Create();
        env.Throw(new InvalidOperationException("boom"));
        var bus = new WolverineMessageBusFake();
        var listener = new LookupStreamingLocationsListener(env.Handler, bus);

        await listener.Handle(Command(), null!);
        var message = bus.SentMessages.Single().Should().BeOfType<MusicCatalogLookupAttemptedDto>().Subject;

        message.Outcome.Status.Should().Be("Failed");
        message.SourceProvider.Should().Be(ProviderName.Odesli.Value);
        message.Outcome.Reason.Should().Be("Lookup failed");
    }

    private static LookupStreamingLocationsCommandDto Command() =>
        new(
            CommandId.For("LookupStreamingLocations:mc_track_1").Value,
            "mc_track_1",
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            new StreamingLocationSearchTermDto("isrc-1", null, null, null),
            null,
            null);
}

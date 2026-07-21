using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Tests.Integration.Worker.Shared;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupStreamingLocations;

public sealed class LookupStreamingLocationDecoratorIntegrationTests
{
    [Fact]
    public async Task Given_A_Duplicate_Isrc_Command_When_Handling_Then_The_Duplicate_Does_Not_Consume_Extra_Budget()
    {
        await using var environment = await LookupExecutionAdmissionDecoratorIntegrationTestEnvironment.CreateAsync();
        var trackId = TestTrackIds.Create("streaming-track-01");
        var subject = new AdmittedLookupHandlerDecorator<LookupStreamingLocationByIsrcMessage>(
            new IsrcInnerHandler(),
            new LookupStreamingLocationByIsrcDecoratorMetadata(),
            environment.CommandBus,
            environment.AdmissionPort,
            environment.Clock);
        var request = new LookupStreamingLocationByIsrcMessage(
            MessageId.For($"lookup:streaming-isrc:Spotify:{trackId.Value}"),
            CorrelationId.From("corr:isrc-a"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            trackId,
            ProviderName.Spotify);
        var otherTrackId = TestTrackIds.Create("streaming-track-02");
        var other = new LookupStreamingLocationByIsrcMessage(
            MessageId.For($"lookup:streaming-isrc:Spotify:{otherTrackId.Value}"),
            CorrelationId.From("corr:isrc-b"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            otherTrackId,
            ProviderName.Spotify);

        await AssertDuplicateDoesNotConsumeExtraBudget(environment, request, other, subject);
    }

    [Fact]
    public async Task Given_A_Duplicate_Metadata_Command_When_Handling_Then_The_Duplicate_Does_Not_Consume_Extra_Budget()
    {
        await using var environment = await LookupExecutionAdmissionDecoratorIntegrationTestEnvironment.CreateAsync();
        var trackId = TestTrackIds.Create("streaming-track-03");
        var subject = new AdmittedLookupHandlerDecorator<LookupStreamingLocationByTrackMetadataMessage>(
            new MetadataInnerHandler(),
            new LookupStreamingLocationByTrackMetadataDecoratorMetadata(),
            environment.CommandBus,
            environment.AdmissionPort,
            environment.Clock);
        var request = new LookupStreamingLocationByTrackMetadataMessage(
            MessageId.For($"lookup:streaming-metadata:Spotify:{trackId.Value}"),
            CorrelationId.From("corr:metadata-a"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            trackId,
            ProviderName.Spotify);
        var otherTrackId = TestTrackIds.Create("streaming-track-04");
        var other = new LookupStreamingLocationByTrackMetadataMessage(
            MessageId.For($"lookup:streaming-metadata:Spotify:{otherTrackId.Value}"),
            CorrelationId.From("corr:metadata-b"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            otherTrackId,
            ProviderName.Spotify);

        await AssertDuplicateDoesNotConsumeExtraBudget(environment, request, other, subject);
    }

    private static async Task AssertDuplicateDoesNotConsumeExtraBudget<TMessage>(
        LookupExecutionAdmissionDecoratorIntegrationTestEnvironment environment,
        TMessage request,
        TMessage other,
        IHandler<TMessage> subject)
        where TMessage : IMessage
    {
        await environment.AdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.Odesli, request.Id, environment.Clock.UtcNow),
            CancellationToken.None);

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<LookupExecutionShortCircuitException>();
        environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>()
            .Which.Result.Should().BeOfType<LookupResult.Duplicate>();

        environment.Clock.UtcNow = environment.Clock.UtcNow.AddSeconds(1);
        var distinct = await environment.AdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.Odesli, other.Id, environment.Clock.UtcNow),
            CancellationToken.None);
        distinct.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
    }

    private sealed class IsrcInnerHandler : IHandler<LookupStreamingLocationByIsrcMessage>
    {
        public Task Handle(LookupStreamingLocationByIsrcMessage request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MetadataInnerHandler : IHandler<LookupStreamingLocationByTrackMetadataMessage>
    {
        public Task Handle(LookupStreamingLocationByTrackMetadataMessage request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

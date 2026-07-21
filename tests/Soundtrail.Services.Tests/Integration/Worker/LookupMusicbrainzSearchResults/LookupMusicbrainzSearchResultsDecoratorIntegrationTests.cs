using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Tests.Integration.Worker.Shared;

namespace Soundtrail.Services.Tests.Integration.Worker.LookupMusicbrainzSearchResults;

public sealed class LookupMusicbrainzSearchResultsDecoratorIntegrationTests
{
    [Fact]
    public async Task Given_A_Duplicate_Search_Command_When_Handling_Then_The_Duplicate_Does_Not_Consume_Extra_Budget()
    {
        await using var environment = await LookupExecutionAdmissionDecoratorIntegrationTestEnvironment.CreateAsync();
        var subject = new AdmittedLookupHandlerDecorator<LookupMusicbrainzSearchResultsMessage>(
            new InnerHandler(),
            new LookupMusicbrainzSearchResultsDecoratorMetadata(),
            environment.CommandBus,
            environment.AdmissionPort,
            environment.Clock);
        var request = new LookupMusicbrainzSearchResultsMessage(
            MessageId.For("lookup:musicbrainz-search:search:u2"),
            CorrelationId.From("corr:search-u2"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            new SearchCriteria("u2", SearchType.Artist));
        var other = new LookupMusicbrainzSearchResultsMessage(
            MessageId.For("lookup:musicbrainz-search:search:abba"),
            CorrelationId.From("corr:search-abba"),
            environment.Clock.UtcNow,
            LookupPriorityBand.High,
            new SearchCriteria("abba", SearchType.Artist));

        await environment.AdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.MusicBrainz, request.Id, environment.Clock.UtcNow),
            CancellationToken.None);

        var action = () => subject.Handle(request);

        await action.Should().ThrowAsync<LookupExecutionShortCircuitException>();
        environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>()
            .Which.Result.Should().BeOfType<LookupResult.Duplicate>();

        environment.Clock.UtcNow = environment.Clock.UtcNow.AddSeconds(1);
        var distinct = await environment.AdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.MusicBrainz, other.Id, environment.Clock.UtcNow),
            CancellationToken.None);
        distinct.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
    }

    private sealed class InnerHandler : IHandler<LookupMusicbrainzSearchResultsMessage>
    {
        public Task Handle(LookupMusicbrainzSearchResultsMessage request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

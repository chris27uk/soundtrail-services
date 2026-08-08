using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Orchestrator.OnUnknownMusicDataRequested;

internal sealed class OnUnknownMusicDataRequestedHandlerUnitTestEnvironment
{
    private OnUnknownMusicDataRequestedHandlerUnitTestEnvironment(
        SearchForCandidatesFake searchForCandidates,
        EventStreamRepositoryFake repository)
    {
        SearchForCandidates = searchForCandidates;
        Repository = repository;
    }

    public SearchForCandidatesFake SearchForCandidates { get; }

    public EventStreamRepositoryFake Repository { get; }

    public static OnUnknownMusicDataRequestedHandlerUnitTestEnvironment Create() =>
        new(new SearchForCandidatesFake(), new EventStreamRepositoryFake());

    public OnUnknownMusicDataRequestedHandler CreateSubject() => new(new WorkPlanner(), SearchForCandidates, Repository);

    public static RequestUnknownMusicDataMessage CreateUnknownRequest(
        string query = "radiohead",
        SearchType searchType = SearchType.Artist,
        LookupPriorityBand priority = LookupPriorityBand.High,
        int trustLevel = 100,
        int riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string commandId = "cmd-unknown",
        string correlationId = "corr-unknown") =>
        new(
            new SearchCriteria(query, searchType),
            priority,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero))
        {
            Id = MessageId.For(commandId),
            CorrelationId = CorrelationId.From(correlationId)
        };

}

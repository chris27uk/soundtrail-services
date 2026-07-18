using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;
using Soundtrail.Services.Internal.Projector.Features.OnMusicDataRequested;

namespace Soundtrail.Services.Tests.Unit.Projector.OnMusicDataRequested;

internal sealed class WorkRequestedProjectorUnitTestEnvironment
{
    private WorkRequestedProjectorUnitTestEnvironment(CommandBusFake commandBus)
    {
        CommandBus = commandBus;
    }

    public CommandBusFake CommandBus { get; }

    public static WorkRequestedProjectorUnitTestEnvironment Create() => new(new CommandBusFake());

    public WorkRequestedProjectorHandler CreateSubject() => new(CommandBus);

    public static WorkRequested CreateSearchCriteriaWorkRequested(
        string query = "u2",
        SearchType searchType = SearchType.Artist,
        LookupPriorityBand priority = LookupPriorityBand.High,
        int? trustLevel = 100,
        int? riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string correlationId = "correlation-1") =>
        new(
            new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria(query, searchType)),
            priority,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 15, 8, 11, 0, TimeSpan.Zero),
            CorrelationId.From(correlationId));

    public sealed class CommandBusFake : ICommandBus
    {
        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }
    }
}

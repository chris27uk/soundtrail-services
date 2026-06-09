using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public sealed class LookupMusicRequestListener(LookupMusicRequestHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        LookupMusicRequestDto requestDto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var request = new LookupMusicRequest(
            NormalizedSearchQuery.FromText(requestDto.Query),
            requestDto.TrustLevel,
            requestDto.RiskScore,
            requestDto.OccurredAt,
            Soundtrail.Contracts.Common.CorrelationId.From(requestDto.CorrelationId));
        var result = await handler.ScheduleAsync(request, cancellationToken);
        return result.Commands.Select(command => command.ToMessage()).ToArray();
    }
}

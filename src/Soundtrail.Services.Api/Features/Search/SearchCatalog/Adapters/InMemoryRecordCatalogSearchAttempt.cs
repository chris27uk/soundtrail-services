using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;

public sealed class InMemoryRecordCatalogSearchAttempt(
    IQueueCatalogSearchAttemptPort? queueCatalogSearchAttemptPort = null) : IRecordCatalogSearchAttemptPort
{
    private readonly HashSet<string> criterias = [];

    public List<RecordCatalogSearchAttemptCommand> Requests { get; } = [];

    public void Seed(CatalogSearchCriteria criteria) => criterias.Add(criteria.Value);

    public async Task<bool> TryRequestAsync(
        RecordCatalogSearchAttemptCommand command,
        CancellationToken cancellationToken)
    {
        if (!criterias.Add(command.Criteria.Value))
        {
            return false;
        }

        Requests.Add(command);
        if (queueCatalogSearchAttemptPort is not null)
        {
            await queueCatalogSearchAttemptPort.EnqueueAsync(command.Request, cancellationToken);
        }

        return true;
    }
}

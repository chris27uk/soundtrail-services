using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

internal static class LookupExecutionReportMappings
{
    public static LookupExecutionReportDto ToReport(
        this LookupExecutionResult result,
        IMusicCatalogLookupCommand command,
        string sourceProvider) =>
        new(
            command.CommandId.Value,
            command.MusicCatalogId.Value,
            sourceProvider,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId.Value,
            result.Outcome.ToString(),
            result.Reason,
            result.RetryAt,
            result.RetryAfterSeconds);
}

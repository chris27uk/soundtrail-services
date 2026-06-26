using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

public sealed record LookupExecutionAdmissionRequest(
    ProviderName Provider,
    CommandId CommandId,
    DateTimeOffset RequestedAt);

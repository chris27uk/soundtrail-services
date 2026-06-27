using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

public sealed record LookupExecutionAdmissionRequest(
    LookupSource Provider,
    CommandId CommandId,
    DateTimeOffset RequestedAt);

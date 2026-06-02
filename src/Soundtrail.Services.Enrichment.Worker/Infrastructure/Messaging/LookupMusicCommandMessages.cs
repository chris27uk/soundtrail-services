using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed record HighPriorityLookupMusicCommandMessage(LookupMusicCommand Command);

public sealed record LowPriorityLookupMusicCommandMessage(LookupMusicCommand Command);

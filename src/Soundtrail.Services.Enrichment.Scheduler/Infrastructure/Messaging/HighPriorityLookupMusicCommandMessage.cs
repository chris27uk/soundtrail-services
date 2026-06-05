using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.Shared.Queuing;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed record HighPriorityLookupMusicCommandMessage(LookupMusicCommand Command);
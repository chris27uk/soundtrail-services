using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed record ExecuteMusicBrainzLookupCommandMessage(ExecuteLookupMusicCommand Command);

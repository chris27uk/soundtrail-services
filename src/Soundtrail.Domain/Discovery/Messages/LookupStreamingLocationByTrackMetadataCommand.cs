using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record LookupStreamingLocationByTrackMetadataCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    TrackId TrackId,
    ProviderName Provider) : ICommand;

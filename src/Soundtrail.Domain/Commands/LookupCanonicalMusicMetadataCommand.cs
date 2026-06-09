using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record LookupCanonicalMusicMetadataCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    CanonicalMusicMetadataLookup Lookup)
{
    public ProviderName TargetProvider => ProviderName.MusicBrainz;
}

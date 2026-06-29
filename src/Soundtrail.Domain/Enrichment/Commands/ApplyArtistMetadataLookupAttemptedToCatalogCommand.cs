using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Domain.Enrichment.Commands;

public sealed record ApplyArtistMetadataLookupAttemptedToCatalogCommand(
    ArtistMetadataLookupAttempted Attempted) : ICommand;

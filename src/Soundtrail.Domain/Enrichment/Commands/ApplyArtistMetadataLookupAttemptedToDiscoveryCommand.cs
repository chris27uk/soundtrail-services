using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Domain.Enrichment.Commands;

public sealed record ApplyArtistMetadataLookupAttemptedToDiscoveryCommand(
    ArtistMetadataLookupAttempted Attempted) : ICommand;

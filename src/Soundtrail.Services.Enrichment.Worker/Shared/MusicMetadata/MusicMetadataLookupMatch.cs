using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

public static class MusicMetadataLookupMatch
{
    public static string Normalize(string? value) => MusicIdentityText.NormalizeFreeText(value);

    public static bool TitleAndArtistMatch(
        string? expectedTitle,
        string? expectedArtist,
        string? actualTitle,
        string? actualArtist) =>
        Normalize(expectedTitle) == Normalize(actualTitle)
        && Normalize(expectedArtist) == Normalize(actualArtist);
}

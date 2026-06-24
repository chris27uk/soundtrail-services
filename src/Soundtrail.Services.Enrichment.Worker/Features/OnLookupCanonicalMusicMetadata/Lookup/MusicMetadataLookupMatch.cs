namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupCanonicalMusicMetadata.Lookup;

public static class MusicMetadataLookupMatch
{
    public static string Normalize(string? value) => Domain.Model.MusicIdentityText.NormalizeFreeText(value);

    public static bool TitleAndArtistMatch(
        string? expectedTitle,
        string? expectedArtist,
        string? actualTitle,
        string? actualArtist) =>
        Normalize(expectedTitle) == Normalize(actualTitle)
        && Normalize(expectedArtist) == Normalize(actualArtist);
}

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump.Support;

internal static class DumpCatalogRows
{
    public const string ArtistA = """{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}""";
    public const string ArtistB = """{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}""";
    public const string BadArtist = """{"name":"Missing Id"}""";

    public const string ReleaseGroupSingle =
        """{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}]}""";

    public const string ReleaseGroupMulti =
        """{"id":"rg222222-2222-2222-2222-222222222222","title":"Collab Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}},{"artist":{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}}]}""";

    public const string BadReleaseGroup =
        """{"title":"Missing Id","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}}]}""";

    public const string TrackSingle =
        """{"id":"rec111111-1111-1111-1111-111111111111","title":"Solo Song","length":210000,"artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}],"release-group":{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album"},"release-date":"2020-05-01"}""";

    public const string TrackMulti =
        """{"id":"rec222222-2222-2222-2222-222222222222","title":"Collab Song","length":180000,"artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}},{"artist":{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}}],"release-group":{"id":"rg222222-2222-2222-2222-222222222222","title":"Collab Album"},"release-date":"2021-06-15"}""";

    public const string BadTrack =
        """{"title":"Missing Id","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}],"release-group":{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album"}}""";
}

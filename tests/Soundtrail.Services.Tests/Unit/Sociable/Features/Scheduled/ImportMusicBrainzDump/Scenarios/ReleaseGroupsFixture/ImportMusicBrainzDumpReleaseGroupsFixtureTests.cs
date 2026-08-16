using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.ReleaseGroupsFixture;

public sealed class ImportMusicBrainzDumpReleaseGroupsFixtureTests
{
    private const string ArtistA = """{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}""";
    private const string ArtistB = """{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}""";
    private const string ReleaseGroupSingle = """{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}]}""";
    private const string ReleaseGroupMulti = """{"id":"rg222222-2222-2222-2222-222222222222","title":"Collab Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}},{"artist":{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}}]}""";
    private const string BadReleaseGroup = """{"title":"Missing Id","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}}]}""";

    [Fact]
    public async Task Given_Fixture_Release_Groups_When_Processed_Then_ReleaseGroups_Shards_Are_Published()
    {
        using var environment = Fixture(ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.SentShards.Count(shard => shard.Phase == MusicBrainzDumpImportPhase.ReleaseGroups)
            .Should().Be(2);
    }

    [Fact]
    public async Task Given_Fixture_Release_Groups_When_Processed_Then_Albums_Are_Imported()
    {
        using var environment = Fixture(ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Fixture_Release_Groups_When_Processed_Then_Imported_Album_Has_MusicBrainz_Source_Id()
    {
        using var environment = Fixture(ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Single().SourceSystemIds
            .Should().Contain(SourceSystemId.MusicBrainz("rg111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task Given_Fixture_Release_Groups_When_Processed_Then_Imported_Album_Title_Comes_From_The_Dump()
    {
        using var environment = Fixture(ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Single().AlbumTitle.Should().Be("Solo Album");
    }

    [Fact]
    public async Task Given_A_Multi_Credit_Release_Group_When_Processed_Then_An_Album_Is_Imported_Per_Credited_Artist()
    {
        using var environment = Fixture(ReleaseGroupMulti);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_Fixture_Release_Groups_When_Processed_Then_The_Job_Is_Completed()
    {
        using var environment = Fixture(ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .Status.Should().Be(MusicBrainzDumpImportJobStatus.Completed);
    }

    [Fact]
    public async Task Given_A_Bad_Release_Group_Row_When_Processed_Then_Only_Valid_Albums_Are_Imported()
    {
        using var environment = Fixture(ReleaseGroupSingle, BadReleaseGroup);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Should().ContainSingle();
    }

    private static ImportMusicBrainzDumpSociableTestEnvironment Fixture(params string[] releaseGroups) =>
        ImportMusicBrainzDumpSociableTestEnvironment.ForDumpContainingArtistsAndAlbums(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            [ArtistA, ArtistB],
            releaseGroups);
}

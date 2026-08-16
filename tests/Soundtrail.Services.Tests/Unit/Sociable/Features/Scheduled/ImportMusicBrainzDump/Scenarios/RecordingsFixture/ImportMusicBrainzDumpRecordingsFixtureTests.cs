using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.RecordingsFixture;

public sealed class ImportMusicBrainzDumpRecordingsFixtureTests
{
    private const string ArtistA = """{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}""";
    private const string ArtistB = """{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}""";
    private const string ReleaseGroupSingle = """{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}]}""";
    private const string ReleaseGroupMulti = """{"id":"rg222222-2222-2222-2222-222222222222","title":"Collab Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}},{"artist":{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}}]}""";
    private const string TrackSingle = """{"id":"rec111111-1111-1111-1111-111111111111","title":"Solo Song","length":210000,"artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}],"release-group":{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album"},"release-date":"2020-05-01"}""";
    private const string TrackMulti = """{"id":"rec222222-2222-2222-2222-222222222222","title":"Collab Song","length":180000,"artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}},{"artist":{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}}],"release-group":{"id":"rg222222-2222-2222-2222-222222222222","title":"Collab Album"},"release-date":"2021-06-15"}""";
    private const string BadTrack = """{"title":"Missing Id","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}],"release-group":{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album"}}""";

    [Fact]
    public async Task Given_Fixture_Tracks_When_Processed_Then_Recordings_Shards_Are_Published()
    {
        using var environment = Fixture([TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.SentShards.Count(shard => shard.Phase == MusicBrainzDumpImportPhase.Recordings)
            .Should().Be(2);
    }

    [Fact]
    public async Task Given_Fixture_Tracks_When_Processed_Then_Tracks_Are_Imported()
    {
        using var environment = Fixture([TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_Fixture_Tracks_When_Processed_Then_Imported_Track_Has_MusicBrainz_Source_Id()
    {
        using var environment = Fixture([TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Single().SourceSystemIds
            .Should().Contain(SourceSystemId.MusicBrainz("rec111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task Given_Fixture_Tracks_When_Processed_Then_Imported_Track_Title_Comes_From_The_Dump()
    {
        using var environment = Fixture([TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Single().Title.Should().Be("Solo Song");
    }

    [Fact]
    public async Task Given_A_Multi_Credit_Track_When_Processed_Then_A_Track_Is_Imported_Per_Credited_Artist()
    {
        using var environment = Fixture([TrackMulti], [ReleaseGroupMulti]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_Fixture_Tracks_When_Processed_Then_The_Job_Is_Completed()
    {
        using var environment = Fixture([TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        var job = environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"));
        job.Status.Should().Be(MusicBrainzDumpImportJobStatus.Completed);
        job.ProgressPercent.Should().Be(100);
    }

    [Fact]
    public async Task Given_A_Bad_Track_Row_When_Processed_Then_Only_Valid_Tracks_Are_Imported()
    {
        using var environment = Fixture([TrackSingle, BadTrack]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Should().ContainSingle();
    }

    private static ImportMusicBrainzDumpSociableTestEnvironment Fixture(
        IReadOnlyList<string> tracks,
        IReadOnlyList<string>? releaseGroups = null) =>
        ImportMusicBrainzDumpSociableTestEnvironment.ForDumpContainingArtistsAlbumsAndTracks(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            [ArtistA, ArtistB],
            releaseGroups ?? [ReleaseGroupSingle],
            tracks);
}

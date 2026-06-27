using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicTrackProjectionStore;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class MusicTrackProjectionStoreResponsesTests
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Stream_With_Resolved_Metadata_And_Apple_Reference_When_Projected_Then_The_Track_Becomes_Playable(ProjectionStoreMode mode)
    {
        await using var env = ProjectionStoreTestEnvironment.Create(mode);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var stream = new MusicTrackStream(2,
        [
            new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)),
            new ProviderReferenceDiscovered(ProviderName.AppleMusic, "apple-1", new Uri("https://music.apple.com/us/song/song-a?i=apple-1"), LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero))
        ]);

        await env.StoreAsync(musicCatalogId, stream);

        var projection = await env.LoadAsync(musicCatalogId);
        projection!.Title.Should().Be("Song A");
        projection.Artist.Should().Be("Artist A");
        projection.AppleId.Should().Be("apple-1");
        projection.IsPlayable.Should().BeTrue();
    }

    public static IEnumerable<object[]> AllModes()
    {
        yield return [ProjectionStoreMode.InProcessFake];
        yield return [ProjectionStoreMode.RavenEmbedded];
    }

    public enum ProjectionStoreMode
    {
        InProcessFake,
        RavenEmbedded
    }

    private sealed class ProjectionStoreTestEnvironment : IAsyncDisposable
    {
        private readonly MusicTrackProjectionStoreFake? fake;
        private readonly RavenEmbeddedTestDatabase? raven;

        private ProjectionStoreTestEnvironment(MusicTrackProjectionStoreFake? fake, RavenEmbeddedTestDatabase? raven)
        {
            this.fake = fake;
            this.raven = raven;
        }

        public static ProjectionStoreTestEnvironment Create(ProjectionStoreMode mode) =>
            mode switch
            {
                ProjectionStoreMode.InProcessFake => new(new MusicTrackProjectionStoreFake(), null),
                ProjectionStoreMode.RavenEmbedded => new(null, RavenEmbeddedTestDatabase.Create()),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

        public async Task StoreAsync(MusicCatalogId musicCatalogId, MusicTrackStream stream)
        {
            if (fake is not null)
            {
                var handler = new MusicTrackChangedHandler(fake, fake);
                await handler.Handle(
                    new MusicTrackChangedCommand(
                        musicCatalogId,
                        stream.Events.Select((@event, index) => new VersionedMusicTrackEvent(index + 1, @event)).ToArray()),
                    CancellationToken.None);
                return;
            }

            await StoreRavenAsync(musicCatalogId, stream);
        }

        private async Task StoreRavenAsync(MusicCatalogId musicCatalogId, MusicTrackStream stream)
        {
            using var session = raven!.Store.OpenAsyncSession();
            var handler = new MusicTrackChangedHandler(
                new RavenLoadMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()),
                new RavenSaveMusicTrackProjection(session, Soundtrail.Services.Internal.Projector.Infrastructure.Translations.InternalProjectorTypeTranslator.Default));
            await handler.Handle(
                new MusicTrackChangedCommand(
                    musicCatalogId,
                    stream.Events.Select((@event, index) => new VersionedMusicTrackEvent(index + 1, @event)).ToArray()),
                CancellationToken.None);
            await session.SaveChangesAsync();
        }

        public async Task<ProjectedTrack?> LoadAsync(MusicCatalogId musicCatalogId)
        {
            if (fake is not null)
            {
                fake.Projections.TryGetValue(musicCatalogId.Value, out var projection);
                return projection is null
                    ? null
                    : new ProjectedTrack(
                        projection.ResolvedMetadata?.Title,
                        projection.ResolvedMetadata?.Artist.Value,
                        projection.AppleReference?.ExternalId,
                        projection.IsPlayable);
            }

            using var session = raven!.Store.OpenAsyncSession();
            var document = await session.LoadAsync<RavenTrackRecordDto>(RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value));
            return document is null
                ? null
                : new ProjectedTrack(document.Title, document.Artist, document.AppleId, document.IsPlayable);
        }

        public ValueTask DisposeAsync()
        {
            raven?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed record ProjectedTrack(string? Title, string? Artist, string? AppleId, bool IsPlayable);
}

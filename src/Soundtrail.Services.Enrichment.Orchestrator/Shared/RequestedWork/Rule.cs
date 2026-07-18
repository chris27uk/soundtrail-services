using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.RequestedWork;

public static class Rule
{
    public static IWorkRule On<T>(Func<T, IReadOnlyList<EnrichmentTarget>> then) => new TypedWorkRule<T>(then);

    public static TypedCatalogItemRuleBuilder<CatalogItemId.Track> WhenTrack() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemId.Artist> WhenArtist() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemId.Album> WhenAlbum() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemId.Playlist> WhenPlaylist() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemOperation.StreamingLocationForTrack> WhenStreamingLocationForTrack() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemOperation.ChildAlbumsForArtist> WhenChildAlbumsForArtist() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemOperation.ChildTracksForArtist> WhenChildTracksForArtist() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemOperation.ChildTracksForAlbum> WhenChildTracksForAlbum() => new();

    public static TypedCatalogItemRuleBuilder<CatalogItemOperation.ChildTracksForPlaylist> WhenChildTracksForPlaylist() => new();

    private sealed class TypedWorkRule<T>(Func<T, IReadOnlyList<EnrichmentTarget>> then) : IWorkRule
    {
        public IReadOnlyList<EnrichmentTarget> Apply(object input) => input is T typed ? then(typed) : [];
    }

    public sealed class TypedCatalogItemRuleBuilder<TInput> : IWorkRule
    {
        private readonly List<Func<TInput, EnrichmentTarget>> steps = [];

        public TypedCatalogItemRuleBuilder<TInput> Then(Func<TInput, EnrichmentTarget> then)
        {
            steps.Add(then);
            return this;
        }

        public TypedCatalogItemRuleBuilder<TInput> And(Func<TInput, EnrichmentTarget> then)
        {
            steps.Add(then);
            return this;
        }

        public IWorkRule ThenNone() => this;

        public IReadOnlyList<EnrichmentTarget> Apply(object input) =>
            input is TInput typed
                ? steps.Select(step => step(typed)).ToArray()
                : [];
    }
}

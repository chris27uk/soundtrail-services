using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Xunit;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class AsyncLookupHappyPathScenario : IAsyncLifetime
{
    private AsyncLookupHappyPathTestEnvironment? env;

    public SearchHttpResponseDto FirstSearchResponse { get; private set; } = null!;

    public SearchHttpResponseDto ResolvedSearchResponse { get; private set; } = null!;

    public LookupMusicRequestDto LookupMusicRequest { get; private set; } = null!;

    public LookupCanonicalMusicMetadataCommandDto LookupCanonicalMusicMetadataCommand { get; private set; } = null!;

    public EnrichmentResponseDto EnrichmentResponse { get; private set; } = null!;

    public PlaybackReferencesResolutionRequiredMessageDto PlaybackReferencesResolutionRequired { get; private set; } = null!;

    public ResolvePlaybackReferencesCommandDto ResolvePlaybackReferencesCommand { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        env = await AsyncLookupHappyPathTestEnvironment.CreateAsync();

        FirstSearchResponse = await env.SearchAndWaitForPipelineAsync("rare unknown song", TimeSpan.FromSeconds(5));
        LookupMusicRequest = await env.WaitForMessageAsync<LookupMusicRequestDto>(TimeSpan.FromSeconds(1));
        LookupCanonicalMusicMetadataCommand = await env.WaitForMessageAsync<LookupCanonicalMusicMetadataCommandDto>(TimeSpan.FromSeconds(1));
        EnrichmentResponse = await env.WaitForMessageAsync<EnrichmentResponseDto>(TimeSpan.FromSeconds(1));
        PlaybackReferencesResolutionRequired = await env.WaitForMessageAsync<PlaybackReferencesResolutionRequiredMessageDto>(TimeSpan.FromSeconds(1));
        ResolvePlaybackReferencesCommand = await env.WaitForMessageAsync<ResolvePlaybackReferencesCommandDto>(TimeSpan.FromSeconds(1));
        ResolvedSearchResponse = await env.WaitForPlayableSearchAsync("rare unknown song", TimeSpan.FromSeconds(15));
    }

    public async Task DisposeAsync()
    {
        if (env is not null)
        {
            await env.DisposeAsync();
        }
    }
}

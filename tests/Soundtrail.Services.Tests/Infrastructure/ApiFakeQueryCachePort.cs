using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Integration.Features.Search
{
    public sealed class ApiFakeQueryCachePort : IQueryCachePort
    {
        private readonly Dictionary<string, Soundtrail.Services.Features.Search.SearchMusicResponse> _responses = new();

        public Task<Soundtrail.Services.Features.Search.SearchMusicResponse?> GetAsync(
            NormalizedSearchQuery query,
            CancellationToken cancellationToken)
        {
            this._responses.TryGetValue(query.Value, out var response);
            return Task.FromResult(response);
        }

        public Task StoreAsync(
            NormalizedSearchQuery query,
            Soundtrail.Services.Features.Search.SearchMusicResponse response,
            TimeSpan timeToLive,
            CancellationToken cancellationToken)
        {
            this._responses[query.Value] = response;
            return Task.CompletedTask;
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}

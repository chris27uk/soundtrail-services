using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Integration.Features.Search
{
    public sealed class ApiFakeResolutionDemandPort : IResolutionDemandPort
    {
        private readonly Dictionary<string, QueryId> _queries = new();

        public IReadOnlyCollection<string> RecordedQueries => this._queries.Keys.ToArray();

        public Task<QueryId> RecordDemandAsync(
            NormalizedSearchQuery query,
            CancellationToken cancellationToken)
        {
            if (!this._queries.TryGetValue(query.Value, out var queryId))
            {
                queryId = QueryId.New();
                this._queries.Add(query.Value, queryId);
            }

            return Task.FromResult(queryId);
        }
    }
}

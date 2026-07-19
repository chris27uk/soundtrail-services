using Dunet;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery
{
    [Union]
    public partial record LookupResult
    {
        public partial record Succeeded(
            LookupResultContext Context,
            LookedUpData Value,
            DateTimeOffset CompletedAt);

        public partial record Duplicate(
            LookupResultContext Context,
            CatalogItem ExistingItem,
            string Reason,
            DateTimeOffset CompletedAt);

        public partial record NotFound(
            LookupResultContext Context,
            string Reason,
            DateTimeOffset CompletedAt);

        public partial record Deferred(
            LookupResultContext Context,
            DateTimeOffset DeferredUntil,
            string Reason,
            DateTimeOffset CompletedAt);

        public partial record Failed(
            LookupResultContext Context,
            string Reason,
            DateTimeOffset CompletedAt);
    }
}

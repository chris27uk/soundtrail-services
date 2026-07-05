using Dunet;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery
{
    [Union]
    public partial record LookupResult
    {
        public partial record Data(LookedUpData Value);
        
        public partial record Duplicate(LookupCriteria Criteria, CatalogItem Value);

        public partial record NotFound(LookupCriteria Criteria);
        
        public partial record Deferred(LookupCriteria Criteria, DateTimeOffset DeferredUntil);
    }
}

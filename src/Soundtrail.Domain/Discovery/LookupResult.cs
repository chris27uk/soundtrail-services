using Dunet;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery
{
    [Union]
    public partial record LookupResult
    {
        public partial record Data(LookedUpData Value);
        
        public partial record Duplicate(SearchCriteria Criteria, CatalogItem Value);

        public partial record NotFound(SearchCriteria Criteria);
        
        public partial record Deferred(SearchCriteria Criteria, DateTimeOffset DeferredUntil);
    }
}

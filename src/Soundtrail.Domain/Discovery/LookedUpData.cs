using Dunet;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery
{
    [Union]
    public partial record LookedUpData
    {
        public partial record Item(CatalogItem Value);

        public partial record StreamingLink(StreamingLocation Value);
    }
}

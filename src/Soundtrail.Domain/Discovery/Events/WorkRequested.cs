using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events
{
    public record WorkRequested(CatalogItemId CatalogItemId);
}

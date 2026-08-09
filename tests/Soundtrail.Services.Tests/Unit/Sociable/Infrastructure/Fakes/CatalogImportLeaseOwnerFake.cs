using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class CatalogImportLeaseOwnerFake(string value) : ICatalogImportLeaseOwner
{
    public static CatalogImportLeaseOwnerFake Default { get; } = new("test-lease-owner");

    public string Value { get; } = value;
}

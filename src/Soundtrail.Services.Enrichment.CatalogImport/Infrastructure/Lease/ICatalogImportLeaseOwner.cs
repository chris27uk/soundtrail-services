namespace Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

/// <summary>
/// Process-scoped lease owner identity. Not restart-stable on purpose: recovery is TTL-based.
/// </summary>
public interface ICatalogImportLeaseOwner
{
    string Value { get; }
}

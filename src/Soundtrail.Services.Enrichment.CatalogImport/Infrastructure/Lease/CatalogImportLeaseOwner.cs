using System.Security.Cryptography;

namespace Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;

public sealed class CatalogImportLeaseOwner : ICatalogImportLeaseOwner
{
    public CatalogImportLeaseOwner()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        Value = Convert.ToHexStringLower(bytes);
    }

    public string Value { get; }
}

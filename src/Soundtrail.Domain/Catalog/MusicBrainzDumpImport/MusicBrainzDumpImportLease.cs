namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public sealed record MusicBrainzDumpImportLease(string Owner, DateTimeOffset ExpiresAt)
{
    public bool IsActive(DateTimeOffset now) =>
        !string.IsNullOrWhiteSpace(Owner) && ExpiresAt > now;
}

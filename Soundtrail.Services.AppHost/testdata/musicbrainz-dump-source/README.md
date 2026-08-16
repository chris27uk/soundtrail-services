# MusicBrainz dump HTTP source (Aspire simulator)

Place MetaBrainz-layout archives here:

```text
{DumpVersion}/artist.tar.xz
{DumpVersion}/release-group.tar.xz
{DumpVersion}/release.tar.xz
```

Smoke archives for `2026-08` are committed for a quick local demo. For a real multi-GB exercise, download official JSON dumps from MetaBrainz into the matching version folder (do not commit multi-GB archives). CatalogImport pulls them over HTTP from this simulator into a local/blob cache; it does not treat this directory as its ArchiveDirectory origin.

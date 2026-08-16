using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.CompositionRoot;

public static class MusicBrainzDumpStorageRegistration
{
    public static bool UsesBlobStorage(IConfiguration configuration)
    {
        var storage = configuration.GetSection(MusicBrainzDumpOptions.SectionName)["Storage"];
        return string.Equals(storage, MusicBrainzDumpOptions.BlobStorage, StringComparison.OrdinalIgnoreCase);
    }

    public static void AddMusicBrainzDumpBlobInfrastructure(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MusicBrainzDumpOptions>>().Value;
            var connectionString = ResolveBlobConnectionString(options, configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "MusicBrainzDump:Storage=Blob requires MusicBrainzDump:BlobConnectionString or ConnectionStrings:musicbrainz-dumps.");
            }

            var containerName = string.IsNullOrWhiteSpace(options.BlobContainerName)
                ? MusicBrainzDumpOptions.DefaultBlobContainerName
                : options.BlobContainerName.Trim();
            return new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
        });

        services.TryAddSingleton<IMusicBrainzDumpBlobContainer>(sp =>
            new AzureMusicBrainzDumpBlobContainer(sp.GetRequiredService<BlobContainerClient>()));
    }

    public static Func<IServiceProvider, IMusicBrainzDumpArchiveStore> CreateArchiveStoreFactory(
        IConfiguration configuration) =>
        UsesBlobStorage(configuration)
            ? sp => ActivatorUtilities.CreateInstance<BlobMusicBrainzDumpArchiveStore>(sp)
            : sp => ActivatorUtilities.CreateInstance<LocalMusicBrainzDumpArchiveStore>(sp);

    public static Func<IServiceProvider, IMusicBrainzDumpShardStore> CreateShardStoreFactory(
        IConfiguration configuration) =>
        UsesBlobStorage(configuration)
            ? sp => ActivatorUtilities.CreateInstance<BlobMusicBrainzDumpShardStore>(sp)
            : sp => ActivatorUtilities.CreateInstance<LocalMusicBrainzDumpShardStore>(sp);

    private static string? ResolveBlobConnectionString(
        MusicBrainzDumpOptions options,
        IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(options.BlobConnectionString))
        {
            return options.BlobConnectionString;
        }

        return configuration.GetConnectionString(MusicBrainzDumpOptions.DefaultBlobConnectionName);
    }
}

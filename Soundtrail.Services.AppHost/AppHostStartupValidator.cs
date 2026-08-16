using Microsoft.Extensions.Configuration;

namespace Soundtrail.Services.AppHost;

public static class AppHostStartupValidator
{
    public static void Validate(IConfiguration configuration, string contentRootPath)
    {
        ValidateServiceBusConnectionString(configuration);
        ValidateRavenDbLicense(configuration);
        ValidateServiceBusEmulator(configuration, contentRootPath);
        ValidateBlobStorage(configuration);
        ValidateMusicBrainzDumpSource(configuration, contentRootPath);
        ValidateWireMockMappings(configuration, contentRootPath);
    }

    private static void ValidateRavenDbLicense(IConfiguration configuration)
    {
        var licensePath = configuration["RavenDb:LicensePath"];
        if (string.IsNullOrWhiteSpace(licensePath))
        {
            return;
        }

        if (!File.Exists(licensePath))
        {
            throw new InvalidOperationException(
                $"RavenDb:LicensePath was set, but no file was found at '{licensePath}'.");
        }
    }

    private static void ValidateServiceBusConnectionString(IConfiguration configuration)
    {
        var useServiceBusEmulator = configuration.GetValue("LocalDevelopment:UseServiceBusEmulator", false);
        if (useServiceBusEmulator)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("servicebus");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Aspire requires a real ConnectionStrings:servicebus value. Replace the placeholder value in AppHost development settings or user secrets.");
        }

        if (connectionString.Contains("UseDevelopmentEmulator=true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (connectionString.Contains("replace-me", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Aspire requires a real ConnectionStrings:servicebus value. Replace the placeholder value in AppHost development settings or user secrets.");
        }
    }

    private static void ValidateServiceBusEmulator(IConfiguration configuration, string contentRootPath)
    {
        var useServiceBusEmulator = configuration.GetValue("LocalDevelopment:UseServiceBusEmulator", false);
        if (!useServiceBusEmulator)
        {
            return;
        }

        var configPath = Path.Combine(contentRootPath, "servicebus-emulator", "Config.json");
        if (!File.Exists(configPath))
        {
            throw new InvalidOperationException(
                $"LocalDevelopment:UseServiceBusEmulator is enabled, but the Service Bus emulator config file was not found at '{configPath}'.");
        }

        var sqlPassword = configuration["ServiceBusEmulator:SqlPassword"];
        if (string.IsNullOrWhiteSpace(sqlPassword))
        {
            throw new InvalidOperationException(
                "LocalDevelopment:UseServiceBusEmulator is enabled, but ServiceBusEmulator:SqlPassword is missing.");
        }
    }

    private static void ValidateBlobStorage(IConfiguration configuration)
    {
        var useBlobStorageEmulator = configuration.GetValue("LocalDevelopment:UseBlobStorageEmulator", false);
        if (useBlobStorageEmulator)
        {
            return;
        }

        var storage = configuration["MusicBrainzDump:Storage"];
        if (!string.Equals(storage, "Blob", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var connectionString = configuration["MusicBrainzDump:BlobConnectionString"]
            ?? configuration.GetConnectionString("musicbrainz-dumps");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "MusicBrainzDump:Storage=Blob without LocalDevelopment:UseBlobStorageEmulator requires MusicBrainzDump:BlobConnectionString or ConnectionStrings:musicbrainz-dumps.");
        }
    }

    private static void ValidateMusicBrainzDumpSource(IConfiguration configuration, string contentRootPath)
    {
        var sourceDirectory = Path.GetFullPath(
            configuration["MusicBrainzDump:DumpSourceDirectory"]
            ?? Path.Combine(contentRootPath, "testdata", "musicbrainz-dump-source"));

        var latestPointerPath = Path.Combine(sourceDirectory, "LATEST");
        string snapshotId;
        if (File.Exists(latestPointerPath))
        {
            snapshotId = File.ReadAllText(latestPointerPath).Trim();
            if (string.IsNullOrWhiteSpace(snapshotId) ||
                string.Equals(snapshotId, "LATEST", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"MusicBrainz dump source LATEST pointer at '{latestPointerPath}' must contain a concrete snapshot directory name.");
            }
        }
        else
        {
            snapshotId = Directory.EnumerateDirectories(sourceDirectory)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(name => !string.Equals(name, "LATEST", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"MusicBrainz dump source at '{sourceDirectory}' requires a LATEST pointer file or at least one snapshot directory.");
        }

        var versionRoot = Path.Combine(sourceDirectory, snapshotId);
        if (!Directory.Exists(versionRoot))
        {
            throw new InvalidOperationException(
                $"MusicBrainz dump source snapshot directory was not found at '{versionRoot}' (from LATEST/discovery).");
        }

        RequireArchive(versionRoot, "artist.tar.xz");
        RequireArchive(versionRoot, "release-group.tar.xz");

        var releaseArchive = Path.Combine(versionRoot, "release.tar.xz");
        var trackArchive = Path.Combine(versionRoot, "track.tar.xz");
        if (!File.Exists(releaseArchive) && !File.Exists(trackArchive))
        {
            throw new InvalidOperationException(
                $"MusicBrainz dump source requires '{releaseArchive}' or '{trackArchive}'. Place MetaBrainz-layout archives under the dump-source mount (smoke files are committed; multi-GB dumps are operator-supplied).");
        }
    }

    private static void RequireArchive(string versionRoot, string fileName)
    {
        var path = Path.Combine(versionRoot, fileName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"MusicBrainz dump source archive was not found at '{path}'.");
        }
    }

    private static void ValidateWireMockMappings(IConfiguration configuration, string contentRootPath)
    {
        var useProviderStubs = configuration.GetValue("LocalDevelopment:UseProviderStubs", false);
        if (!useProviderStubs)
        {
            return;
        }

        var mappingsPath = Path.Combine(contentRootPath, "wiremock", "mappings");
        if (!Directory.Exists(mappingsPath))
        {
            throw new InvalidOperationException(
                $"LocalDevelopment:UseProviderStubs is enabled, but the WireMock mappings directory was not found at '{mappingsPath}'.");
        }

        var hasMappings = Directory.EnumerateFiles(mappingsPath, "*.json", SearchOption.TopDirectoryOnly).Any();
        if (!hasMappings)
        {
            throw new InvalidOperationException(
                $"LocalDevelopment:UseProviderStubs is enabled, but no WireMock mapping files were found at '{mappingsPath}'.");
        }
    }
}

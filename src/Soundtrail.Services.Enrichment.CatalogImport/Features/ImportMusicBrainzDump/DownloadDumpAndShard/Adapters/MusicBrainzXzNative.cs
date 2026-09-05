using System.Runtime.InteropServices;
using Joveler.Compression.XZ;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

internal static class MusicBrainzXzNative
{
    private static readonly object Gate = new();
    private static bool initialized;

    public static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        lock (Gate)
        {
            if (initialized)
            {
                return;
            }

            var bundled = ResolveBundledNativePath();
            if (bundled is not null)
            {
                XZInit.GlobalInit(bundled);
            }
            else
            {
                XZInit.GlobalInit();
            }

            initialized = true;
        }
    }

    private static string? ResolveBundledNativePath()
    {
        var rid = GetRuntimeIdentifierFolder();
        if (rid is null)
        {
            return null;
        }

        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "liblzma.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "liblzma.dylib"
                : "liblzma.so";

        var candidate = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", fileName);
        return File.Exists(candidate) ? candidate : null;
    }

    private static string? GetRuntimeIdentifierFolder()
    {
        string os;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            os = "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            os = "osx";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            os = "linux";
        }
        else
        {
            return null;
        }

        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => null
        };

        return arch is null ? null : $"{os}-{arch}";
    }
}

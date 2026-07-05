using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.ServiceDefaults;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Infrastructure.CompositionRoot;

if (!MusicBrainzDumpCommandLine.TryParse(args, out var command, out var error))
{
    Console.WriteLine(MusicBrainzDumpCommandLine.Usage());

    if (!string.IsNullOrWhiteSpace(error))
    {
        Console.Error.WriteLine(error);
        return 1;
    }

    return 0;
}

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddMusicBrainzImportAppServices(builder.Configuration);

using var host = builder.Build();
await host.StartAsync();

var runner = host.Services.GetRequiredService<MusicBrainzDumpCommandLineRunner>();
var exitCode = await runner.RunAsync(command!, CancellationToken.None);

await host.StopAsync();
return exitCode;

using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UseMusicTrackLookupCoordinatorServiceBusMessaging(builder.Configuration));
builder.Services.AddMusicTrackLookupCoordinatorAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();

using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.ListTracksByAlbum.Adapters;
using Soundtrail.Services.Api.Features.ListTracksByArtist.Adapters;
using Soundtrail.Services.Api.Features.SearchCatalog.Adapters;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UseApiServiceBusMessaging(builder.Configuration, builder.Environment));
builder.Services.AddApiAppServices(builder.Configuration, builder.Environment);

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapSearchCatalogEndpoints();
app.MapGetArtistEndpoints();
app.MapListTracksByArtistEndpoints();
app.MapGetAlbumEndpoints();
app.MapListTracksByAlbumEndpoints();
app.MapGetTrackEndpoints();
app.Run();

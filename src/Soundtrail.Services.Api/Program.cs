using Soundtrail.Services.Api.Features.Albums;
using Soundtrail.Services.Api.Features.Artists;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Tracks;
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
app.MapSearchEndpoints();
app.MapArtistEndpoints();
app.MapAlbumEndpoints();
app.MapTrackEndpoints();
app.Run();

using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Internal.Projector.Infrastructure.CompositionRoot;
using Soundtrail.Services.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddInternalProjectorAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();

using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Catalog.Projector.Infrastructure.CompositionRoot;
using Soundtrail.Services.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddCatalogProjectorAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();

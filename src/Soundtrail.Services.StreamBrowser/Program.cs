using System.Text.Json.Serialization;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Services.ServiceDefaults;
using Soundtrail.Services.StreamBrowser;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRavenDocumentStore(builder.Configuration);
builder.Services.AddSingleton<StreamBrowseService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapDefaultEndpoints();

app.MapGet("/api/meta", () => Results.Ok(new
{
    streamKinds =
        new object[]
        {
            new
            {
                id = StreamKinds.Work,
                title = "Work streams",
                aggregateType = StreamKinds.WorkAggregateType,
                description = "Discovery / enrichment work history (catalog-stream). Keyed by CatalogWorkId.",
                metadataPrefix = StreamKinds.MetadataPrefix(StreamKinds.Work),
                eventPrefixPattern = "catalog-stream-events/{streamId}/{version}"
            },
            new
            {
                id = StreamKinds.Catalog,
                title = "Artist catalog streams",
                aggregateType = StreamKinds.CatalogAggregateType,
                description = "Per-artist catalog facts (artist-catalog-stream). Keyed by ArtistId.",
                metadataPrefix = StreamKinds.MetadataPrefix(StreamKinds.Catalog),
                eventPrefixPattern = "artist-catalog-stream-events/{streamId}/{version}"
            }
        },
    keyingTemplates = StreamKeyBuilder.Templates
}));

app.MapPost("/api/keys/build", (BuildKeyRequest request) =>
{
    try
    {
        var values = request.Values ?? new Dictionary<string, string?>();
        var result = StreamKeyBuilder.Build(request.TemplateId, values);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/streams", async (
    StreamBrowseService browse,
    string kind = StreamKinds.Work,
    string? q = null,
    int skip = 0,
    int take = 0,
    CancellationToken cancellationToken = default) =>
{
    if (!IsKnownKind(kind))
    {
        return Results.BadRequest(new { error = "kind must be 'work' or 'catalog'." });
    }

    var result = await browse.ListStreamsAsync(kind, q, skip, take, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/streams/{kind}/{*streamId}", async (
    StreamBrowseService browse,
    string kind,
    string streamId,
    CancellationToken cancellationToken) =>
{
    if (!IsKnownKind(kind))
    {
        return Results.BadRequest(new { error = "kind must be 'work' or 'catalog'." });
    }

    var result = await browse.GetStreamAsync(kind, streamId, cancellationToken);
    return result is null ? Results.NotFound(new { error = "Stream not found." }) : Results.Ok(result);
});

app.MapFallbackToFile("index.html");
app.Run();

static bool IsKnownKind(string kind) =>
    kind.Equals(StreamKinds.Work, StringComparison.OrdinalIgnoreCase) ||
    kind.Equals(StreamKinds.Catalog, StringComparison.OrdinalIgnoreCase);

internal sealed record BuildKeyRequest(string TemplateId, Dictionary<string, string?>? Values);

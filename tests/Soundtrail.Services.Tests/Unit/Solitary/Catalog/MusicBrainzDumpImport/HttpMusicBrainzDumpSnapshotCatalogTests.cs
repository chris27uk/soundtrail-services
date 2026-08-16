using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Adapters;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class HttpMusicBrainzDumpSnapshotCatalogTests
{
    [Fact]
    public async Task Given_Latest_Pointer_When_Resolving_Then_Concrete_Id_Is_Returned()
    {
        var handler = new SnapshotHttpHandler
        {
            LatestBody = "20260808-001002"
        };
        var catalog = CreateCatalog(handler);

        var latest = await catalog.GetLatestSnapshotIdAsync();

        latest.Value.Should().Be("20260808-001002");
    }

    [Fact]
    public async Task Given_No_Latest_When_Resolving_Then_Newest_Directory_Is_Used()
    {
        var handler = new SnapshotHttpHandler
        {
            LatestStatus = HttpStatusCode.NotFound,
            IndexHtml = """
                <a href="20260801-001001/">20260801-001001/</a>
                <a href="20260808-001002/">20260808-001002/</a>
                <a href="LATEST">LATEST</a>
                """
        };
        var catalog = CreateCatalog(handler);

        var latest = await catalog.GetLatestSnapshotIdAsync();

        latest.Value.Should().Be("20260808-001002");
    }

    [Fact]
    public async Task Given_Required_Archives_When_Checking_Exists_Then_It_Is_True()
    {
        var handler = new SnapshotHttpHandler { HeadOk = true };
        var catalog = CreateCatalog(handler);

        var exists = await catalog.SnapshotExistsAsync(MusicBrainzDumpSnapshotId.Parse("2026-08"));

        exists.Should().BeTrue();
        handler.HeadPaths.Should().BeEquivalentTo(
            "/json-dumps/2026-08/artist.tar.xz",
            "/json-dumps/2026-08/release-group.tar.xz",
            "/json-dumps/2026-08/release.tar.xz");
    }

    [Fact]
    public async Task Given_Missing_Archive_When_Checking_Exists_Then_It_Is_False()
    {
        var handler = new SnapshotHttpHandler { HeadOk = false };
        var catalog = CreateCatalog(handler);

        var exists = await catalog.SnapshotExistsAsync(MusicBrainzDumpSnapshotId.Parse("2026-08"));

        exists.Should().BeFalse();
    }

    private static HttpMusicBrainzDumpSnapshotCatalog CreateCatalog(SnapshotHttpHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test") },
            Options.Create(new MusicBrainzDumpOptions
            {
                BaseUrl = "https://example.test/json-dumps"
            }));

    private sealed class SnapshotHttpHandler : HttpMessageHandler
    {
        public string? LatestBody { get; init; }

        public HttpStatusCode LatestStatus { get; init; } = HttpStatusCode.OK;

        public string IndexHtml { get; init; } = string.Empty;

        public bool HeadOk { get; init; } = true;

        public List<string> HeadPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/LATEST", StringComparison.Ordinal))
            {
                return Task.FromResult(
                    new HttpResponseMessage(LatestStatus)
                    {
                        Content = new StringContent(LatestBody ?? string.Empty, Encoding.UTF8)
                    });
            }

            if (request.Method == HttpMethod.Head)
            {
                HeadPaths.Add(path);
                return Task.FromResult(
                    new HttpResponseMessage(HeadOk ? HttpStatusCode.OK : HttpStatusCode.NotFound));
            }

            if (path.EndsWith("/json-dumps/") || path.EndsWith("/json-dumps", StringComparison.Ordinal))
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(IndexHtml, Encoding.UTF8, "text/html")
                    });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}

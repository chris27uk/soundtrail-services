using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Responses;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;

internal sealed class WireMockMusicProvidersServer : IDisposable
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<StubResponse>> responsesByPath = new();
    private readonly HttpListener server = new();
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task serverLoop;

    public WireMockMusicProvidersServer()
    {
        BaseUrl = StartServer(server);
        serverLoop = Task.Run(() => RunServerLoopAsync(shutdown.Token));
    }

    public string BaseUrl { get; }

    public static WireMockMusicProvidersServer Create() => new();

    public static WireMockMusicProvidersServer CreateForAsyncLookupHappyPath()
    {
        var server = new WireMockMusicProvidersServer();
        server.SeedAsyncLookupHappyPath();
        return server;
    }

    public void SeedMusicBrainz(MusicSearchCriteria lookup, SongMetadata metadata)
    {
        lookup.Match(
            (track, artist, album) =>
            {
                EnqueueResponse("/ws/2/recording", BuildRecordingSearchResponse(
                    (
                        metadata.Mbid,
                        metadata.Title,
                        metadata.DurationMs,
                        "100",
                        Array.Empty<string>(),
                        metadata.Artist,
                        metadata.SourceArtistId,
                        metadata.AlbumTitle ?? album,
                        metadata.SourceAlbumId,
                        metadata.ReleaseDate?.ToString("yyyy-MM-dd"))));

                return 0;
            },
            isrc =>
            {
                EnqueueResponse($"/ws/2/isrc/{Uri.EscapeDataString(isrc)}", BuildIsrcLookupResponse(
                    (metadata.Mbid, metadata.Title, metadata.DurationMs, new[] { metadata.Isrc ?? isrc }, metadata.Artist, metadata.SourceArtistId, metadata.AlbumTitle, metadata.SourceAlbumId, metadata.ReleaseDate?.ToString("yyyy-MM-dd"))));

                return 0;
            });
    }

    public void SeedMusicBrainzSearchResponse(params (string? Mbid, string Title, int? DurationMs, string Score, string[] Isrcs, string Artist, string? ArtistSourceId, string? ReleaseTitle, string? ReleaseSourceId, string? ReleaseDate)[] recordings) =>
        EnqueueResponse("/ws/2/recording", BuildRecordingSearchResponse(recordings));

    public void SeedMusicBrainzIsrcResponse(string isrc, params (string? Mbid, string Title, int? DurationMs, string[] Isrcs, string Artist, string? ArtistSourceId, string? ReleaseTitle, string? ReleaseSourceId, string? ReleaseDate)[] recordings) =>
        EnqueueResponse($"/ws/2/isrc/{Uri.EscapeDataString(isrc)}", BuildIsrcLookupResponse(recordings));

    public void SeedOdesli(MusicSearchCriteria searchCriteria, IReadOnlyList<ExternalReference> references, string userCountry = "US")
    {
        var youtube = references.SingleOrDefault(x => x.Provider == ProviderName.YoutubeMusic);
        var spotify = references.SingleOrDefault(x => x.Provider == ProviderName.Spotify);
        var apple = references.SingleOrDefault(x => x.Provider == ProviderName.AppleMusic);
        var body = $$"""
        {
          "linksByPlatform": {
            {{(youtube is null ? string.Empty : $@"""youtubeMusic"":{{""url"":""{youtube.Url}""}}")}}
            {{(youtube is not null && (spotify is not null || apple is not null) ? "," : string.Empty)}}
            {{(spotify is null ? string.Empty : $@"""spotify"":{{""url"":""{spotify.Url}""}}")}}
            {{(spotify is not null && apple is not null ? "," : string.Empty)}}
            {{(apple is null ? string.Empty : $@"""appleMusic"":{{""url"":""{apple.Url}""}}")}}
          }
        }
        """;

        EnqueueResponse("/v1-user/links", body);
    }

    public void Dispose()
    {
        shutdown.Cancel();
        server.Stop();
        server.Close();

        try
        {
            serverLoop.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpListenerException)
        {
        }

        shutdown.Dispose();
    }

    private void SeedAsyncLookupHappyPath()
    {
        var metadata = new SongMetadata(
            "Rare Unknown Song",
            "Test Artist",
            "isrc-rare-1",
            "mbid-rare-1",
            123000,
            "Rare Album",
            new DateOnly(2026, 1, 1),
            "mb-artist-rare-1",
            "mb-release-rare-1");
        var references =
            new ExternalReference[]
            {
                new(
                    ProviderName.AppleMusic,
                    new Uri("https://music.apple.com/track/apple-track-1?i=apple-track-1"),
                    "apple-track-1"),
                new(
                    ProviderName.YoutubeMusic,
                    new Uri("https://music.youtube.com/watch?v=yt-track-1"),
                    "yt-track-1")
            };

        SeedMusicBrainz(
            MusicSearchCriteria.ByTrackArtistAlbum("Rare Unknown Song", "Test Artist", "Rare Album"),
            metadata);
        SeedOdesli(MusicSearchCriteria.ByIsrc("isrc-rare-1"), references);
        SeedOdesli(MusicSearchCriteria.ByTrackArtistAlbum("Rare Unknown Song", "Test Artist", "Rare Album"), references);
        SeedOdesli(MusicSearchCriteria.ByTrackArtistAlbum("Rare Unknown Song", "Test Artist", null), references);
    }

    private void EnqueueResponse(string path, string body)
    {
        var queue = responsesByPath.GetOrAdd(path, _ => new ConcurrentQueue<StubResponse>());
        queue.Enqueue(new StubResponse(HttpStatusCode.OK, "application/json", body));
    }

    private static string BuildRecordingSearchResponse(
        params (string? Mbid, string Title, int? DurationMs, string Score, string[] Isrcs, string Artist, string? ArtistSourceId, string? ReleaseTitle, string? ReleaseSourceId, string? ReleaseDate)[] recordings) =>
        $$"""
        {
          "recordings": [
            {{string.Join(",",
                recordings.Select(recording =>
                    $$"""
                    {
                      "id": "{{recording.Mbid}}",
                      "title": "{{recording.Title}}",
                      "length": {{recording.DurationMs?.ToString() ?? "null"}},
                      "score": "{{recording.Score}}",
                      "isrcs": [{{string.Join(",", recording.Isrcs.Select(isrc => $"\"{isrc}\""))}}],
                      "artist-credit": [
                        { "name": "{{recording.Artist}}", "artist": { "id": "{{recording.ArtistSourceId}}" } }
                      ],
                      "releases": [{{BuildRelease(recording.ReleaseTitle, recording.ReleaseSourceId, recording.ReleaseDate)}}]
                    }
                    """))}}
          ]
        }
        """;

    private static string BuildIsrcLookupResponse(
        params (string? Mbid, string Title, int? DurationMs, string[] Isrcs, string Artist, string? ArtistSourceId, string? ReleaseTitle, string? ReleaseSourceId, string? ReleaseDate)[] recordings) =>
        $$"""
        {
          "recordings": [
            {{string.Join(",",
                recordings.Select(recording =>
                    $$"""
                    {
                      "id": "{{recording.Mbid}}",
                      "title": "{{recording.Title}}",
                      "length": {{recording.DurationMs?.ToString() ?? "null"}},
                      "isrcs": [{{string.Join(",", recording.Isrcs.Select(isrc => $"\"{isrc}\""))}}],
                      "artist-credit": [
                        { "name": "{{recording.Artist}}", "artist": { "id": "{{recording.ArtistSourceId}}" } }
                      ],
                      "releases": [{{BuildRelease(recording.ReleaseTitle, recording.ReleaseSourceId, recording.ReleaseDate)}}]
                    }
                    """))}}
          ]
        }
        """;

    private static string BuildRelease(string? releaseTitle, string? releaseSourceId, string? releaseDate)
    {
        if (string.IsNullOrWhiteSpace(releaseTitle))
        {
            return string.Empty;
        }

        return $$"""
        {
          "id": "{{releaseSourceId}}",
          "title": "{{releaseTitle}}",
          "date": {{(string.IsNullOrWhiteSpace(releaseDate) ? "null" : $"\"{releaseDate}\"")}}
        }
        """;
    }

    private async Task RunServerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await server.GetContextAsync();
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await WriteResponseAsync(context, cancellationToken);
        }
    }

    private async Task WriteResponseAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var path = context.Request.Url?.AbsolutePath ?? "/";

        if (!responsesByPath.TryGetValue(path, out var queue) || !queue.TryDequeue(out var response))
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.Close();
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(response.Body);
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = response.ContentType;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, cancellationToken);
        context.Response.Close();
    }

    private static string StartServer(HttpListener server)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var port = GetAvailablePort();
            var baseUrl = $"http://127.0.0.1:{port}";

            try
            {
                server.Prefixes.Clear();
                server.Prefixes.Add($"{baseUrl}/");
                server.Start();
                return baseUrl;
            }
            catch (HttpListenerException) when (attempt < 19)
            {
                if (server.IsListening)
                {
                    server.Stop();
                }
            }
        }

        throw new InvalidOperationException("Unable to start WireMockMusicProvidersServer after multiple port binding attempts.");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed record StubResponse(HttpStatusCode StatusCode, string ContentType, string Body);
}

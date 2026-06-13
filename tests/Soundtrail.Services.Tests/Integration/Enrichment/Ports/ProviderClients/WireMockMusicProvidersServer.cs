using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
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
        var port = GetAvailablePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        server.Prefixes.Add($"{BaseUrl}/");
        server.Start();
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

    public void SeedMusicBrainz(MusicSearchTerm lookup, SongMetadata metadata)
    {
        lookup.Match(
            (track, artist, album) =>
            {
                EnqueueResponse(
                    "/ws/2/recording",
                    $$"""
                    {
                      "recordings": [
                        {
                          "id": "{{metadata.Mbid}}",
                          "title": "{{metadata.Title}}",
                          "length": {{metadata.DurationMs?.ToString() ?? "null"}},
                          "score": "100",
                          "isrcs": [],
                          "artist-credit": [
                            { "name": "{{metadata.Artist}}" }
                          ]
                        }
                      ]
                    }
                    """);

                return 0;
            },
            isrc =>
            {
                EnqueueResponse(
                    $"/ws/2/isrc/{Uri.EscapeDataString(isrc)}",
                    $$"""
                    {
                      "recordings": [
                        {
                          "id": "{{metadata.Mbid}}",
                          "title": "{{metadata.Title}}",
                          "length": {{metadata.DurationMs?.ToString() ?? "null"}},
                          "isrcs": ["{{metadata.Isrc}}"],
                          "artist-credit": [
                            { "name": "{{metadata.Artist}}" }
                          ]
                        }
                      ]
                    }
                    """);

                return 0;
            });
    }

    public void SeedOdesli(MusicSearchTerm searchTerm, IReadOnlyList<ExternalReference> references, string userCountry = "US")
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
            123000);
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
            MusicSearchTerm.ByTrackArtistAlbum("Rare Unknown Song", "Test Artist", "Rare Album"),
            metadata);
        SeedOdesli(MusicSearchTerm.ByIsrc("isrc-rare-1"), references);
        SeedOdesli(MusicSearchTerm.ByTrackArtistAlbum("Rare Unknown Song", "Test Artist", "Rare Album"), references);
        SeedOdesli(MusicSearchTerm.ByTrackArtistAlbum("Rare Unknown Song", "Test Artist", null), references);
    }

    private void EnqueueResponse(string path, string body)
    {
        var queue = responsesByPath.GetOrAdd(path, _ => new ConcurrentQueue<StubResponse>());
        queue.Enqueue(new StubResponse(HttpStatusCode.OK, "application/json", body));
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

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed record StubResponse(HttpStatusCode StatusCode, string ContentType, string Body);
}

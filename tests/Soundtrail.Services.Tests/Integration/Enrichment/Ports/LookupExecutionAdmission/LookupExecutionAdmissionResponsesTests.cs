using FluentAssertions;
using Microsoft.Extensions.Options;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Configuration;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.LookupExecutionAdmission;

public sealed class LookupExecutionAdmissionResponsesTests
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_New_CommandId_When_Acquiring_Then_It_Is_Acquired_Once(LookupExecutionAdmissionMode mode)
    {
        await using var env = await LookupExecutionAdmissionTestEnvironment.CreateAsync(mode);
        var request = LookupRequest("LookupTrackMetadata:track-1");

        var first = await env.Port.TryAcquireAsync(request, CancellationToken.None);
        var second = await env.Port.TryAcquireAsync(request, CancellationToken.None);

        first.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
        second.Status.Should().Be(LookupExecutionAdmissionStatus.Duplicate);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Released_CommandId_When_Acquiring_Again_Then_It_Is_Acquired_Again(LookupExecutionAdmissionMode mode)
    {
        await using var env = await LookupExecutionAdmissionTestEnvironment.CreateAsync(mode);
        var request = LookupRequest("LookupTrackMetadata:track-2");

        var first = await env.Port.TryAcquireAsync(request, CancellationToken.None);
        await env.Port.ReleaseAsync(request.CommandId, CancellationToken.None);
        var second = await env.Port.TryAcquireAsync(request, CancellationToken.None);

        first.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
        second.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Committed_CommandId_When_Acquiring_Again_Then_It_Remains_A_Duplicate(LookupExecutionAdmissionMode mode)
    {
        await using var env = await LookupExecutionAdmissionTestEnvironment.CreateAsync(mode);
        var request = LookupRequest("LookupStreamingLocations:track-3", ProviderName.Odesli);

        await env.Port.TryAcquireAsync(request, CancellationToken.None);
        await env.Port.CommitAsync(request.CommandId, CancellationToken.None);
        var second = await env.Port.TryAcquireAsync(request, CancellationToken.None);

        second.Status.Should().Be(LookupExecutionAdmissionStatus.Duplicate);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Provider_At_Budget_When_Acquiring_Then_It_Is_Deferred(LookupExecutionAdmissionMode mode)
    {
        await using var env = await LookupExecutionAdmissionTestEnvironment.CreateAsync(mode);
        env.RejectAfterSuccesses(ProviderName.MusicBrainz, 1);
        var first = LookupRequest("LookupTrackMetadata:track-4a");
        var second = LookupRequest("LookupTrackMetadata:track-4b");

        var firstResult = await env.Port.TryAcquireAsync(first, CancellationToken.None);
        var secondResult = await env.Port.TryAcquireAsync(second, CancellationToken.None);

        firstResult.Status.Should().Be(LookupExecutionAdmissionStatus.Acquired);
        secondResult.Status.Should().Be(LookupExecutionAdmissionStatus.Deferred);
        secondResult.Reason.Should().Contain("MusicBrainz budget temporarily unavailable");
        secondResult.RetryAt.Should().NotBeNull();
    }

    public static IEnumerable<object[]> AllModes()
    {
        yield return [LookupExecutionAdmissionMode.InProcessFake];
        if (DockerRedisServer.IsAvailable())
        {
            yield return [LookupExecutionAdmissionMode.LocalRedis];
        }
    }

    private static LookupExecutionAdmissionRequest LookupRequest(
        string commandId,
        ProviderName? provider = null) =>
        new(
            provider ?? ProviderName.MusicBrainz,
            CommandId.For(commandId),
            DateTimeOffset.UtcNow);

    public enum LookupExecutionAdmissionMode
    {
        InProcessFake,
        LocalRedis
    }

    private sealed class LookupExecutionAdmissionTestEnvironment : IAsyncDisposable
    {
        private readonly IAsyncDisposable? asyncDisposable;

        private LookupExecutionAdmissionTestEnvironment(
            ILookupExecutionAdmissionPort port,
            IAsyncDisposable? asyncDisposable)
        {
            Port = port;
            this.asyncDisposable = asyncDisposable;
        }

        public ILookupExecutionAdmissionPort Port { get; }

        public static async Task<LookupExecutionAdmissionTestEnvironment> CreateAsync(LookupExecutionAdmissionMode mode)
        {
            return mode switch
            {
                LookupExecutionAdmissionMode.InProcessFake => CreateFake(),
                LookupExecutionAdmissionMode.LocalRedis => await CreateRedisAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public async ValueTask DisposeAsync()
        {
            if (asyncDisposable is not null)
            {
                await asyncDisposable.DisposeAsync();
            }
        }

        public void RejectAfterSuccesses(ProviderName provider, int successfulAcquisitions)
        {
            if (Port is LookupExecutionAdmissionPortFake fake)
            {
                fake.RejectAfterSuccesses(provider, successfulAcquisitions);
            }
        }

        private static LookupExecutionAdmissionTestEnvironment CreateFake()
        {
            var fake = new LookupExecutionAdmissionPortFake();
            return new LookupExecutionAdmissionTestEnvironment(fake, null);
        }

        private static async Task<LookupExecutionAdmissionTestEnvironment> CreateRedisAsync()
        {
            var redis = await DockerRedisServer.StartAsync();
            var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.ConnectionString);

            var port = new RedisLookupExecutionAdmissionPort(
                multiplexer,
                Options.Create(new SourceApiBudgetsOptions
                {
                    MusicBrainz = new SourceApiBudgetPolicyOptions
                    {
                        MaxRequests = 1,
                        WindowSeconds = 60,
                        SafetyMarginPercent = 0,
                        MinimumSpacingSeconds = null
                    },
                    Odesli = new SourceApiBudgetPolicyOptions
                    {
                        MaxRequests = 1,
                        WindowSeconds = 60,
                        SafetyMarginPercent = 0,
                        MinimumSpacingSeconds = null
                    }
                }),
                Options.Create(new RedisLookupExecutionAdmissionOptions
                {
                    ActiveLeaseSeconds = 30,
                    KeyPrefix = $"lookup-execution-admission-tests:{Guid.NewGuid():N}"
                }));

            return new LookupExecutionAdmissionTestEnvironment(
                port,
                new AsyncDisposer(async () =>
                {
                    await multiplexer.DisposeAsync();
                    await redis.DisposeAsync();
                }));
        }
    }

    private sealed class DockerRedisServer : IAsyncDisposable
    {
        private readonly string containerId;

        private DockerRedisServer(
            string containerId,
            int port)
        {
            this.containerId = containerId;
            Port = port;
        }

        public int Port { get; }

        public string ConnectionString => $"127.0.0.1:{Port},abortConnect=false";

        public static async Task<DockerRedisServer> StartAsync()
        {
            var port = GetAvailablePort();
            var containerId = await RunDockerAsync(
                $"run --rm -d -p 127.0.0.1:{port}:6379 redis:7-alpine redis-server --save '' --appendonly no");

            var server = new DockerRedisServer(containerId.Trim(), port);

            try
            {
                await server.WaitUntilReadyAsync();
                return server;
            }
            catch
            {
                await server.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (string.IsNullOrWhiteSpace(containerId))
            {
                return;
            }

            try
            {
                await RunDockerAsync($"rm -f {containerId}");
            }
            catch
            {
            }
        }

        private async Task WaitUntilReadyAsync()
        {
            var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(30);

            while (DateTimeOffset.UtcNow < timeoutAt)
            {
                using var client = new TcpClient();

                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, Port);
                    return;
                }
                catch
                {
                    await Task.Delay(250);
                }
            }

            throw new TimeoutException($"Timed out waiting for local Redis container on port {Port}.");
        }

        public static bool IsAvailable()
        {
            try
            {
                var startInfo = new ProcessStartInfo("docker", "info")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    return false;
                }

                process.WaitForExit(3000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<string> RunDockerAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo("docker", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start docker process.");

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Docker command 'docker {arguments}' failed with exit code {process.ExitCode}: {stderr}");
            }

            return stdout.Trim();
        }

        private static int GetAvailablePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }

    private sealed class AsyncDisposer(Func<Task> dispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => new(dispose());
    }
}

using Microsoft.Extensions.Options;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using StackExchange.Redis;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;

internal sealed class RedisLookupExecutionAdmissionPort(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<SourceApiBudgetsOptions> sourceBudgetOptions,
    IOptions<RedisLookupExecutionAdmissionOptions> redisOptions) : ILookupExecutionAdmissionPort
{
    private const string ActiveState = "active";
    private const string CompletedState = "completed";
    private const char StateSeparator = '|';
    private const char ReservationSeparator = ',';

    private static readonly LuaScript ReserveWindowScript = LuaScript.Prepare(
        """
        local requestedAmount = tonumber(@requestedAmount)
        local safeMax = tonumber(@safeMax)
        local ttlMilliseconds = tonumber(@ttlMilliseconds)
        local current = redis.call('INCRBY', @key, requestedAmount)
        if current == requestedAmount then
            redis.call('PEXPIRE', @key, ttlMilliseconds)
        end

        if current > safeMax then
            redis.call('DECRBY', @key, requestedAmount)
            return 0
        end

        return 1
        """);

    private static readonly LuaScript ReleaseIfActiveScript = LuaScript.Prepare(
        """
        local current = redis.call('GET', @key)
        if current == @activeState then
            redis.call('DEL', @key)
            return 1
        end

        return 0
        """);

    private readonly SourceApiBudgetsOptions sourceBudgetOptions = sourceBudgetOptions.Value;
    private readonly RedisLookupExecutionAdmissionOptions redisOptions = redisOptions.Value;

    public async Task<LookupExecutionAdmissionResult> TryAcquireAsync(
        LookupExecutionAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = connectionMultiplexer.GetDatabase();
        var commandKey = GetCommandKey(request.CommandId);
        var acquired = await db.StringSetAsync(
            commandKey,
            ActiveState,
            expiry: TimeSpan.FromSeconds(redisOptions.ActiveLeaseSeconds),
            when: When.NotExists);

        if (!acquired)
        {
            return LookupExecutionAdmissionResult.Duplicate();
        }

        var reservedKeys = new List<RedisKey>();

        try
        {
            var policy = GetPolicy(request.Provider);

            if (policy.MinimumSpacingSeconds is { } minimumSpacingSeconds)
            {
                var spacingReservation = await TryReserveWindowAsync(
                    db,
                    request,
                    maxRequests: 1,
                    safetyMarginPercent: 0,
                    windowSeconds: minimumSpacingSeconds,
                    keyPrefix: "source-budget-spacing",
                    cancellationToken);

                if (!spacingReservation.Reserved)
                {
                    return await RejectAsync(db, commandKey, spacingReservation.RetryAt, request.Provider, cancellationToken);
                }

                reservedKeys.Add(spacingReservation.Key);
            }

            var mainReservation = await TryReserveWindowAsync(
                db,
                request,
                policy.MaxRequests,
                policy.SafetyMarginPercent,
                policy.WindowSeconds,
                "source-budget",
                cancellationToken);

            if (!mainReservation.Reserved)
            {
                await RollbackReservationsAsync(db, reservedKeys);
                return await RejectAsync(db, commandKey, mainReservation.RetryAt, request.Provider, cancellationToken);
            }

            reservedKeys.Add(mainReservation.Key);
            await db.StringSetAsync(
                commandKey,
                SerializeActiveCommand(reservedKeys),
                expiry: TimeSpan.FromSeconds(redisOptions.ActiveLeaseSeconds));

            return LookupExecutionAdmissionResult.Acquired();
        }
        catch
        {
            await RollbackReservationsAsync(db, reservedKeys);
            await db.KeyDeleteAsync(commandKey);
            throw;
        }
    }

    public Task CommitAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return connectionMultiplexer.GetDatabase().StringSetAsync(GetCommandKey(commandId), CompletedState);
    }

    public async Task ReleaseAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = connectionMultiplexer.GetDatabase();
        var commandKey = GetCommandKey(commandId);
        var current = await db.StringGetAsync(commandKey);
        if (!current.HasValue)
        {
            return;
        }

        var payload = current.ToString();
        if (string.Equals(payload, CompletedState, StringComparison.Ordinal))
        {
            return;
        }

        if (!payload.StartsWith(ActiveState, StringComparison.Ordinal))
        {
            await db.KeyDeleteAsync(commandKey);
            return;
        }

        await RollbackReservationsAsync(db, ParseReservedKeys(payload));
        await db.KeyDeleteAsync(commandKey);
    }

    private async Task<WindowReservationResult> TryReserveWindowAsync(
        IDatabase db,
        LookupExecutionAdmissionRequest request,
        int maxRequests,
        int safetyMarginPercent,
        int windowSeconds,
        string keyPrefix,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeMax = Math.Max(1, maxRequests - (int)Math.Floor(maxRequests * (safetyMarginPercent / 100d)));
        var windowStartedAt = AlignToWindow(request.RequestedAt, windowSeconds);
        var windowEndsAt = windowStartedAt.AddSeconds(windowSeconds);
        var ttl = windowEndsAt - request.RequestedAt;
        if (ttl <= TimeSpan.Zero)
        {
            ttl = TimeSpan.FromSeconds(windowSeconds);
        }

        var key = $"{redisOptions.KeyPrefix}:{keyPrefix}:{request.Provider.Value}:{windowStartedAt.ToUnixTimeSeconds()}";

        var reserved = (int)(long)await db.ScriptEvaluateAsync(
            ReserveWindowScript,
            new
            {
                key = (RedisKey)key,
                requestedAmount = 1,
                safeMax,
                ttlMilliseconds = (long)Math.Ceiling(ttl.TotalMilliseconds)
            }) == 1;

        return reserved
            ? WindowReservationResult.Success(key, windowEndsAt)
            : WindowReservationResult.Rejected(key, windowEndsAt);
    }

    private async Task<LookupExecutionAdmissionResult> RejectAsync(
        IDatabase db,
        RedisKey commandKey,
        DateTimeOffset retryAt,
        LookupSource provider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await db.KeyDeleteAsync(commandKey);
        return LookupExecutionAdmissionResult.Deferred(
            retryAt,
            $"{provider.Value} budget temporarily unavailable");
    }

    private async Task RollbackReservationsAsync(IDatabase db, IEnumerable<RedisKey> keys)
    {
        foreach (var key in keys)
        {
            await db.StringDecrementAsync(key);
        }
    }

    private static string SerializeActiveCommand(IEnumerable<RedisKey> reservedKeys) =>
        $"{ActiveState}{StateSeparator}{string.Join(ReservationSeparator, reservedKeys.Select(key => key.ToString()))}";

    private static IReadOnlyList<RedisKey> ParseReservedKeys(string payload)
    {
        var separatorIndex = payload.IndexOf(StateSeparator);
        if (separatorIndex < 0 || separatorIndex == payload.Length - 1)
        {
            return [];
        }

        return payload[(separatorIndex + 1)..]
            .Split(ReservationSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(key => (RedisKey)key)
            .ToArray();
    }

    private SourceApiBudgetPolicyOptions GetPolicy(LookupSource provider) =>
        provider == LookupSource.MusicBrainz
            ? sourceBudgetOptions.MusicBrainz
            : provider == LookupSource.Odesli
                ? sourceBudgetOptions.Odesli
                : throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported source budget.");

    private static DateTimeOffset AlignToWindow(DateTimeOffset timestamp, int windowSeconds)
    {
        var utc = timestamp.ToUniversalTime();
        var ticks = utc.Ticks - (utc.Ticks % TimeSpan.FromSeconds(windowSeconds).Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private string GetCommandKey(CommandId commandId) =>
        $"{redisOptions.KeyPrefix}:command:{commandId.Value}";

    private sealed record WindowReservationResult(bool Reserved, RedisKey Key, DateTimeOffset RetryAt)
    {
        public static WindowReservationResult Success(RedisKey key, DateTimeOffset retryAt) => new(true, key, retryAt);

        public static WindowReservationResult Rejected(RedisKey key, DateTimeOffset retryAt) => new(false, key, retryAt);
    }
}

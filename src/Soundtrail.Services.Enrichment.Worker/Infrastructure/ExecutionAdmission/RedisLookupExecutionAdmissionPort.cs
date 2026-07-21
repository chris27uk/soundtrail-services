using Microsoft.Extensions.Options;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using StackExchange.Redis;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;

internal sealed class RedisLookupExecutionAdmissionPort(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<SourceApiBudgetsOptions> sourceBudgetOptions,
    IOptions<RedisLookupExecutionAdmissionOptions> redisOptions) : ILookupExecutionAdmissionPort
{
    private const string ReservedState = "reserved";
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

    private readonly SourceApiBudgetsOptions sourceBudgetOptions = sourceBudgetOptions.Value;
    private readonly RedisLookupExecutionAdmissionOptions redisOptions = redisOptions.Value;

    public async Task<LookupExecutionAdmissionResult> TryAcquireAsync(
        LookupExecutionAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = connectionMultiplexer.GetDatabase();
        var commandKey = GetCommandKey(request.MessageId);
        var leaseKey = GetLeaseKey(request.MessageId);
        var existingState = await db.StringGetAsync(commandKey);

        if (string.Equals(existingState, CompletedState, StringComparison.Ordinal))
        {
            return LookupExecutionAdmissionResult.Duplicate();
        }

        if (IsReservedState(existingState))
        {
            var reacquiredLease = await TryAcquireLeaseAsync(db, leaseKey);
            return reacquiredLease
                ? LookupExecutionAdmissionResult.Acquired()
                : LookupExecutionAdmissionResult.Duplicate();
        }

        var acquiredLease = await TryAcquireLeaseAsync(db, leaseKey);
        if (!acquiredLease)
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
                    return await RejectAsync(db, leaseKey, spacingReservation.RetryAt, request.Provider, cancellationToken);
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
                return await RejectAsync(db, leaseKey, mainReservation.RetryAt, request.Provider, cancellationToken);
            }

            reservedKeys.Add(mainReservation.Key);
            await db.StringSetAsync(
                commandKey,
                SerializeReservedCommand(reservedKeys));

            return LookupExecutionAdmissionResult.Acquired();
        }
        catch
        {
            await RollbackReservationsAsync(db, reservedKeys);
            await db.KeyDeleteAsync(commandKey);
            await db.KeyDeleteAsync(leaseKey);
            throw;
        }
    }

    public Task CommitAsync(
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = connectionMultiplexer.GetDatabase();
        return CommitAsync(db, messageId);
    }

    public async Task ReleaseAsync(
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = connectionMultiplexer.GetDatabase();
        var commandKey = GetCommandKey(messageId);
        var leaseKey = GetLeaseKey(messageId);
        var current = await db.StringGetAsync(commandKey);
        if (!current.HasValue)
        {
            await db.KeyDeleteAsync(leaseKey);
            return;
        }

        var payload = current.ToString();
        if (string.Equals(payload, CompletedState, StringComparison.Ordinal))
        {
            await db.KeyDeleteAsync(leaseKey);
            return;
        }

        if (!IsReservedState(payload))
        {
            await db.KeyDeleteAsync(commandKey);
            await db.KeyDeleteAsync(leaseKey);
            return;
        }

        await RollbackReservationsAsync(db, ParseReservedKeys(payload));
        await db.KeyDeleteAsync(commandKey);
        await db.KeyDeleteAsync(leaseKey);
    }

    private async Task<bool> TryAcquireLeaseAsync(IDatabase db, RedisKey leaseKey)
    {
        return await db.StringSetAsync(
            leaseKey,
            "1",
            expiry: TimeSpan.FromSeconds(redisOptions.ActiveLeaseSeconds),
            when: When.NotExists);
    }

    private async Task CommitAsync(IDatabase db, MessageId messageId)
    {
        await db.StringSetAsync(GetCommandKey(messageId), CompletedState);
        await db.KeyDeleteAsync(GetLeaseKey(messageId));
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
        RedisKey leaseKey,
        DateTimeOffset retryAt,
        LookupSource provider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await db.KeyDeleteAsync(leaseKey);
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

    private static bool IsReservedState(RedisValue payload) =>
        payload.HasValue && payload.ToString().StartsWith(ReservedState, StringComparison.Ordinal);

    private static string SerializeReservedCommand(IEnumerable<RedisKey> reservedKeys) =>
        $"{ReservedState}{StateSeparator}{string.Join(ReservationSeparator, reservedKeys.Select(key => key.ToString()))}";

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

    private ApiBudgetPolicy GetPolicy(LookupSource provider)
    {
        if (provider == LookupSource.MusicBrainz && this.sourceBudgetOptions.MusicBrainz != null)
        {
            return sourceBudgetOptions.MusicBrainz;
        }

        if (provider == LookupSource.Odesli && this.sourceBudgetOptions.Odesli != null)
        {
            return sourceBudgetOptions.Odesli;
        }

        if (provider == LookupSource.Kworb && this.sourceBudgetOptions.Kworb != null)
        {
            return sourceBudgetOptions.Kworb;
        }
        
        throw new ArgumentOutOfRangeException(nameof(provider));
    }

    private static DateTimeOffset AlignToWindow(DateTimeOffset timestamp, int windowSeconds)
    {
        var utc = timestamp.ToUniversalTime();
        var ticks = utc.Ticks - (utc.Ticks % TimeSpan.FromSeconds(windowSeconds).Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private string GetCommandKey(MessageId messageId) =>
        $"{redisOptions.KeyPrefix}:command:{messageId.Value}";

    private string GetLeaseKey(MessageId messageId) =>
        $"{redisOptions.KeyPrefix}:lease:{messageId.Value}";

    private sealed record WindowReservationResult(bool Reserved, RedisKey Key, DateTimeOffset RetryAt)
    {
        public static WindowReservationResult Success(RedisKey key, DateTimeOffset retryAt) => new(true, key, retryAt);

        public static WindowReservationResult Rejected(RedisKey key, DateTimeOffset retryAt) => new(false, key, retryAt);
    }
}

public class SourceApiBudgetsOptions
{
    public ApiBudgetPolicy? MusicBrainz { get; set; }
    
    public ApiBudgetPolicy? Odesli { get; set; }

    public ApiBudgetPolicy? Kworb { get; set; }
}

public class ApiBudgetPolicy
{
    public int MaxRequests { get; set; }
    public int MinimumSpacingSeconds { get; set; }
    public int SafetyMarginPercent { get; set; }
    public int WindowSeconds { get; set; }
}

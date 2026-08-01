using System.Text;
using Blake2Fast;

namespace Soundtrail.Domain.Common;

public readonly record struct MessageId(string Value)
{
    public static MessageId New() => new(Guid.NewGuid().ToString("N"));

    public static MessageId For(string value) => new(value);

    public static MessageId Deterministic(string prefix, params string?[] parts)
    {
        return new($"{prefix}:{ComputeHash(parts)}");
    }

    public static MessageId DeterministicWithPrefix(string prefix, params string?[] parts) =>
        new($"{prefix}:{ComputeHash(parts)}");

    public static MessageId From(string value) => new(value);

    public override string ToString() => Value;
    
    public static implicit operator string(MessageId commandId) => commandId.Value;
    
    public static implicit operator MessageId(string commandId) => new(commandId);

    private static string ComputeHash(IEnumerable<string?> parts)
    {
        var payload = string.Join(
            "|",
            parts.Select(static part => string.IsNullOrWhiteSpace(part) ? "~" : part));
        var hash = Blake2b.ComputeHash(16, Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }
}

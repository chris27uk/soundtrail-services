using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Abstractions.EventSourcing;

public readonly record struct OperationId(string StableValue) : IValueType
{
    public static OperationId From(string value) => new(value);
}

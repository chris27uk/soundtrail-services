using Soundtrail.Contracts.Common;

namespace Soundtrail.Adapters.EventSourcing;

public readonly record struct OperationId(string StableValue) : IValueType
{
    public static OperationId From(string value) => new(value);
}

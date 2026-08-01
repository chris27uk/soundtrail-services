using System.Text.Json;

namespace Soundtrail.Adapters.Messaging;

internal sealed class SystemTextJsonMessageBodyDeserializer(
    JsonSerializerOptions serializerOptions) : IMessageBodyDeserializer
{
    public TMessage Deserialize<TMessage>(BinaryData body)
    {
        var deserialized = JsonSerializer.Deserialize<TMessage>(body, serializerOptions);
        if (deserialized is null)
        {
            throw new InvalidOperationException(
                $"The message body for '{typeof(TMessage).FullName}' could not be deserialized.");
        }

        return deserialized;
    }
}

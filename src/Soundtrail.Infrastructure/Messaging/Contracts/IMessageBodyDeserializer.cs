namespace Soundtrail.Adapters.Messaging;

public interface IMessageBodyDeserializer
{
    TMessage Deserialize<TMessage>(BinaryData body);
}

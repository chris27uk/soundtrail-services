namespace Soundtrail.Adapters.Messaging.Contracts;

public interface IMessageBodyDeserializer
{
    TMessage Deserialize<TMessage>(BinaryData body);
}

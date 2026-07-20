namespace Soundtrail.Domain.Abstractions
{
    public interface IPrioritisedMessage : IMessage
    {
        public int? RiskScore { get; }
        
        public int? TrustLevel { get; }
    }
}

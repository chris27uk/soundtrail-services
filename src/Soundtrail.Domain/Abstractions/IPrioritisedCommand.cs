namespace Soundtrail.Domain.Abstractions
{
    public interface IPrioritisedCommand : ICommand
    {
        public int? RiskScore { get; }
        
        public int? TrustLevel { get; }
    }
}

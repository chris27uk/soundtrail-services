namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution
{
    public readonly record struct ProviderName
    {
        private ProviderName(string value)
        {
            if (!IsKnown(value))
            {
                throw new ArgumentException($"Unknown provider name '{value}'.", nameof(value));
            }
            
            this.Value = value;
        }
        
        public string Value { get; }

        public static ProviderName From(string value) => new(value);
        
        public static bool IsKnown(string value) =>
            value is "AppleMusic" or "YoutubeMusic" or "MusicBrainz";
        
        public static ProviderName AppleMusic { get; } = new("AppleMusic");

        public static ProviderName YoutubeMusic { get; } = new("YoutubeMusic");

        public static ProviderName MusicBrainz { get; } = new("MusicBrainz");

        public override string ToString() => Value;
    }
}

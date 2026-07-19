namespace Soundtrail.Domain.Common
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
        
        public static bool IsKnown(string value) => value is "AppleMusic" or "YoutubeMusic" or "Spotify";
        
        public static ProviderName AppleMusic { get; } = new("AppleMusic");

        public static ProviderName YoutubeMusic { get; } = new("YoutubeMusic");

        public static ProviderName Spotify { get; } = new("Spotify");

        public override string ToString() => Value;

        public static ProviderName[] All => [Spotify, AppleMusic, YoutubeMusic];
        
        public string StableValue =>
            this.Value switch
            {
                "Spotify" => "spotify",
                "AppleMusic" => "appleMusic",
                "YoutubeMusic" => "youtubeMusic",
                _ => throw new ArgumentException($"Unknown provider name '{this.Value}'.", nameof(this.Value))
            };
    }
}

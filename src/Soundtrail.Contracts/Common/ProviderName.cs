namespace Soundtrail.Contracts.Common
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
            value is "AppleMusic" or "YoutubeMusic" or "MusicBrainz" or "Spotify" or "Odesli";
        
        public static ProviderName AppleMusic { get; } = new("AppleMusic");

        public static ProviderName YoutubeMusic { get; } = new("YoutubeMusic");

        public static ProviderName MusicBrainz { get; } = new("MusicBrainz");

        public static ProviderName Spotify { get; } = new("Spotify");

        public static ProviderName Odesli { get; } = new("Odesli");

        public override string ToString() => Value;

        public string ToPersistentId()
        {
            return this.Value switch
            {
                "Spotify" => "spotify",
                "AppleMusic" => "appleMusic",
                "YoutubeMusic" => "youtubeMusic",
                _ => throw new ArgumentException($"Unknown provider name '{this.Value}'.", nameof(this.Value))
            };
        }
    }
}

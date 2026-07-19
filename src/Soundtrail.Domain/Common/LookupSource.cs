namespace Soundtrail.Domain.Common
{
    public readonly record struct LookupSource
    {
        private LookupSource(string value)
        {
            if (!IsKnown(value))
            {
                throw new ArgumentException($"Unknown lookup source '{value}'.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public static LookupSource From(string value) => new(value);

        public static bool IsKnown(string value) => value is "MusicBrainz" or "Odesli";

        public static LookupSource MusicBrainz { get; } = new("MusicBrainz");

        public static LookupSource Odesli { get; } = new("Odesli");

        public override string ToString() => Value;
    }
}

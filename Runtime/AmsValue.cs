namespace TeaSpoons.AMS
{
    /// <summary>
    /// Represents a parsed AMS value from a string (e.g., "100" or "15%").
    /// </summary>
    public readonly struct AmsValue
    {
        public TransformType Type { get; }
        public double Magnitude { get; }

        /// <summary>
        /// Creates an AmsValue by parsing a string.
        /// Strings ending with '%' are treated as percentages.
        /// </summary>
        /// <param name="rawMagnitude">The value string (e.g., "100" or "15.5%")</param>
        public AmsValue(string rawMagnitude)
        {
            if (string.IsNullOrWhiteSpace(rawMagnitude))
            {
                Type = TransformType.Absolute;
                Magnitude = 0;
                return;
            }

            rawMagnitude = rawMagnitude.Trim();

            if (rawMagnitude.EndsWith('%'))
            {
                Type = TransformType.Percentage;
                Magnitude = double.Parse(rawMagnitude[..^1]);
            }
            else
            {
                Type = TransformType.Absolute;
                Magnitude = double.Parse(rawMagnitude);
            }
        }

        public static AmsValue Absolute(double magnitude) => new(TransformType.Absolute, magnitude);

        public static AmsValue Percentage(double magnitude) => new(TransformType.Percentage, magnitude);

        private AmsValue(TransformType type, double magnitude)
        {
            Type = type;
            Magnitude = magnitude;
        }

        public override string ToString()
        {
            return Type == TransformType.Percentage
                ? $"{Magnitude}%"
                : $"{Magnitude}";
        }
    }
}

namespace TeaSpoons.AMS
{
    using System;
    
    /// <summary>
    /// Immutable snapshot of an attribute's value components.
    /// Contains absolute, percentage, and average values for a single attribute.
    /// </summary>
    public readonly struct AmsAttributeValue : IEquatable<AmsAttributeValue>
    {
        public static readonly AmsAttributeValue Empty = new(0.0, 0.0, 0.0, 0.0);
        
        public double Absolute { get; }
        public double Percentage { get; }
        public double AverageAbsolute { get; }
        public double AveragePercentage { get; }

        public AmsAttributeValue(double absolute, double percentage, double averageAbsolute, double averagePercentage)
        {
            Absolute = absolute;
            Percentage = percentage;
            AverageAbsolute = averageAbsolute;
            AveragePercentage = averagePercentage;
        }

        public bool IsAbsoluteEmpty => Math.Abs(Absolute) < AmsCalculatorProvider.Instance.Epsilon;
        public bool IsPercentageEmpty => Math.Abs(Percentage) < AmsCalculatorProvider.Instance.Epsilon;
        public bool IsAverageAbsoluteEmpty => Math.Abs(AverageAbsolute) < AmsCalculatorProvider.Instance.Epsilon;
        public bool IsAveragePercentageEmpty => Math.Abs(AveragePercentage) < AmsCalculatorProvider.Instance.Epsilon;

        public bool IsEmpty => IsAbsoluteEmpty && IsPercentageEmpty && IsAverageAbsoluteEmpty && IsAveragePercentageEmpty;

        public bool Equals(AmsAttributeValue other)
        {
            return Math.Abs(Absolute - other.Absolute) < AmsCalculatorProvider.Instance.Epsilon &&
                   Math.Abs(Percentage - other.Percentage) < AmsCalculatorProvider.Instance.Epsilon &&
                   Math.Abs(AverageAbsolute - other.AverageAbsolute) < AmsCalculatorProvider.Instance.Epsilon &&
                   Math.Abs(AveragePercentage - other.AveragePercentage) < AmsCalculatorProvider.Instance.Epsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is AmsAttributeValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Absolute, Percentage, AverageAbsolute, AveragePercentage);
        }

        public override string ToString()
        {
            return $"AmsAttributeValue [absolute={Absolute}, percentage={Percentage}%, avgAbs={AverageAbsolute}, avgPct={AveragePercentage}%]";
        }

        public static bool operator ==(AmsAttributeValue left, AmsAttributeValue right) => left.Equals(right);
        public static bool operator !=(AmsAttributeValue left, AmsAttributeValue right) => !left.Equals(right);
    }
}

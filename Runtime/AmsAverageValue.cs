namespace TeaSpoons.AMS
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents a weighted average value for AMS calculations.
    /// Tracks total value and stack count to compute averages.
    /// </summary>
    public class AmsAverageValue : IEquatable<AmsAverageValue>
    {
        public double TotalValue { get; private set; }

        public int StackCount { get; private set; }

        public AmsAverageValue(double totalValue, int stackCount, string context = null)
        {
            if (stackCount <= 0)
            {
                Logs.AMS?.Error?.Log(
                    $"AmsAverageValue ctor: stackCount must be > 0. stackCount={stackCount}, totalValue={totalValue}. Context={context ?? "<null>"}");
                throw new InvalidDataException("[AMS] AmsAverageValue: stackCount must be a positive number.");
            }

            TotalValue = totalValue;
            StackCount = stackCount;
        }

        public AmsAverageValue(AmsAverageValue other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            
            TotalValue = other.TotalValue;
            StackCount = other.StackCount;
        }

        /// <summary>
        /// Adds a value with its stack count to the weighted average.
        /// The value is multiplied by stackCount before adding to total.
        /// </summary>
        /// <param name="value">The value to add (will be multiplied by stackCount)</param>
        /// <param name="stackCount">Number of stacks this value represents (must be >= 1)</param>
        /// <param name="context">used for logging/debugging</param>
        /// <returns>This instance for chaining</returns>
        public AmsAverageValue AddAverage(double value, int stackCount, string context = null)
        {
            var newStackCount = stackCount + StackCount;
            if (newStackCount <= 0)
            {
                Logs.AMS?.Warning?.Log($"AmsAverageValue.AddAverage ignored: stackCount must be > 0. value={value}, " +
                                       $"added stackCount={stackCount}, current stackCount={StackCount}. Context={context ?? "<null>"}");
                return this;
            }

            TotalValue += value * stackCount;
            StackCount = newStackCount;
            return this;
        }

        /// <summary>
        /// Calculates and returns the weighted average.
        /// </summary>
        public double GetAverage(string context = null)
        {
            return TotalValue / StackCount;
        }

        public bool Equals(AmsAverageValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return StackCount == other.StackCount &&
                   Math.Abs(TotalValue - other.TotalValue) < AmsCalculatorProvider.Instance.Epsilon;
        }

        public override bool Equals(object obj) => Equals(obj as AmsAverageValue);

        /// <summary>
        /// This is a combined hashcode of the current values, not persistent and this
        /// class should not be used for lookups
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(StackCount, TotalValue);

        public override string ToString()
        {
            var avg = TotalValue / StackCount;
            return $"AmsAverageValue [totalValue={TotalValue}, stackCount={StackCount}, average={avg}]";
        }

        public static bool operator ==(AmsAverageValue left, AmsAverageValue right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(AmsAverageValue left, AmsAverageValue right) => !(left == right);
    }
}

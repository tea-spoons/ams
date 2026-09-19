namespace TeaSpoons.AMS
{
    /// <summary>
    /// Strategy interface for AMS calculations.
    /// Swap implementations to change calculation behavior without modifying core AMS logic.
    /// </summary>
    public interface IAmsCalculator
    {
        /// <summary>
        /// Adds two values together.
        /// </summary>
        double Add(double a, double b, string context = null);

        /// <summary>
        /// Multiplies two values together.
        /// </summary>
        double Multiply(double a, double b, string context = null);

        /// <summary>
        /// Divides a by b.
        /// Note: Division by (near) zero is undefined; the default implementation should log and throw
        /// so the caller can decide the correct fallback/handling for their domain.
        /// </summary>
        double Divide(double a, double b, string context = null);

        /// <summary>
        /// Applies a percentage modifier to a base value.
        /// Formula: baseValue * (1 + percentage * 0.01)
        /// </summary>
        /// <param name="baseValue">The base value to modify</param>
        /// <param name="percentage">The percentage (e.g., 15.0 means 15%)</param>
        /// <param name="context">Optional context for logging/debugging</param>
        /// <returns>The modified value</returns>
        double ApplyPercentage(double baseValue, double percentage, string context = null);

        /// <summary>
        /// Converts a calculated double value to the final long representation.
        /// Implementation determines truncation vs rounding behavior.
        /// </summary>
        long ToLong(double value, string context = null);

        /// <summary>
        /// Epsilon value for floating-point comparisons.
        /// Two values are considered equal if their difference is less than this.
        /// </summary>
        double Epsilon { get; }
    }
}

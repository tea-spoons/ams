namespace TeaSpoons.AMS
{
    using System;

    /// <summary>
    /// Helper/extension methods for AMS math that let callers choose how to handle edge cases (e.g., division by zero)
    /// </summary>
    public static class AmsMath
    {
        /// <summary>
        /// Returns true if the value is considered (near) zero using the calculator's epsilon.
        /// </summary>
        public static bool IsNearZero(this IAmsCalculator calculator, double value)
        {
            if (calculator is not null)
            {
                return Math.Abs(value) < calculator.Epsilon;
            }
            
            Logs.AMS?.Error?.Log("Calculator is null.");
            throw new ArgumentNullException(nameof(calculator));
        }

        /// <summary>
        /// Attempts to divide a by b.
        /// - If b is (near) zero or calculator is null, returns false, sets result to default, and logs a warning (with optional context).
        /// - Otherwise returns true and sets result to the division result.
        /// </summary>
        public static bool TryDivide(this IAmsCalculator calculator, double a, double b, out double result, string context = null)
        {
            if (calculator is null)
            {
                Logs.AMS?.Warning?.Log($"TryDivide failed (calculator is null): {a} / {b}. Context={context ?? "<null>"}");
                result = default;
                return false;
            }

            if (Math.Abs(b) < calculator.Epsilon)
            {
                Logs.AMS?.Warning?.Log($"TryDivide failed (denominator near zero): {a} / {b}. Context={context ?? "<null>"}");
                result = default;
                return false;
            }

            result = calculator.Divide(a, b, context);
            return true;
        }

        /// <summary>
        /// Divides a by b, or returns the provided fallback if b is (near) zero.
        /// </summary>
        public static double DivideOr(this IAmsCalculator calculator, double a, double b, double fallback, string context = null)
        {
            if (calculator is null)
            {
                Logs.AMS?.Warning?.Log($"DivideOr fallback used (calculator is null): {fallback}. Context={context ?? "<null>"}");
                return fallback;
            }

            if (Math.Abs(b) >= calculator.Epsilon)
            {
                return calculator.Divide(a, b, context);
            }

            Logs.AMS?.Warning?.Log($"DivideOr fallback used (denominator near zero): {a} / {b} => {fallback}. Context={context ?? "<null>"}");
            return fallback;
        }

        /// <summary>
        /// Divides a by b, or returns the fallback value produced by <paramref name="fallbackFactory"/> if b is (near) zero.
        /// </summary>
        public static double DivideOr(this IAmsCalculator calculator, double a, double b, Func<double> fallbackFactory, string context = null)
        {
            if (fallbackFactory is null)
            {
                throw new ArgumentNullException(nameof(fallbackFactory));
            }

            if (calculator is null)
            {
                var fallback = fallbackFactory();
                Logs.AMS?.Warning?.Log($"DivideOr(f) fallback used (calculator is null): {fallback}. Context={context ?? "<null>"}");
                return fallback;
            }

            if (Math.Abs(b) >= calculator.Epsilon)
            {
                return calculator.Divide(a, b, context);
            }
            
            var divideOrFallback = fallbackFactory();
            Logs.AMS?.Warning?.Log($"DivideOr(f) fallback used (denominator near zero): {a} / {b} => {divideOrFallback}. Context={context ?? "<null>"}");
            return divideOrFallback;

        }
    }
}

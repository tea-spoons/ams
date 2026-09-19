namespace TeaSpoons.AMS
{
    using System;

    /// <summary>
    /// Default calculator using simple double arithmetic.
    /// Accepts that minor floating-point drift may occur between client and server.
    /// </summary>
    public class DefaultCalculator : IAmsCalculator
    {
        private const double DefaultEpsilon = 0.0000000001;

        public double Epsilon => DefaultEpsilon;

        public double Add(double a, double b, string context = null) => a + b;

        public double Multiply(double a, double b, string context = null) => a * b;

        /// <summary>
        /// Division is undefined when the denominator is (near) zero.
        /// </summary>
        public double Divide(double a, double b, string context = null)
        {
            if (Math.Abs(b) >= Epsilon)
            {
                return a / b;
            }

            Logs.AMS?.Error?.Log($"Division by zero attempted: {a} / {b}. Context={context ?? "<null>"}");
            throw new DivideByZeroException($"AMS division by zero: {a} / {b}. Context={context ?? "<null>"}");
        }

        public double ApplyPercentage(double baseValue, double percentage, string context = null)
        {
            return baseValue * (1.0 + percentage * 0.01);
        }

        public long ToLong(double value, string context = null) => (long)value;
    }
}

namespace TeaSpoons.AMS
{
    /// <summary>
    /// Provides global access to the AMS calculator.
    /// Allows swapping calculator implementations at runtime.
    /// </summary>
    public static class AmsCalculatorProvider
    {
        private static IAmsCalculator instance;

        /// <summary>
        /// Gets the current calculator instance.
        /// Defaults to DefaultCalculator if not set.
        /// </summary>
        public static IAmsCalculator Instance => instance ??= new DefaultCalculator();

        /// <summary>
        /// Sets the calculator implementation.
        /// Call this early in application startup to change behavior.
        /// </summary>
        /// <param name="calculator">The calculator to use</param>
        public static void SetCalculator(IAmsCalculator calculator)
        {
            instance = calculator ?? throw new System.ArgumentNullException(nameof(calculator));
        }

        /// <summary>
        /// Creates an instrumented calculator wrapping the current instance.
        /// Useful for debugging calculation differences with server.
        /// </summary>
        public static InstrumentedCalculator CreateInstrumented()
        {
            return new InstrumentedCalculator(Instance);
        }

        /// <summary>
        /// Resets to default calculator.
        /// </summary>
        public static void Reset()
        {
            instance = new DefaultCalculator();
        }
    }
}

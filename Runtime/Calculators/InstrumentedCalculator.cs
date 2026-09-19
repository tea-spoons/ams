namespace TeaSpoons.AMS
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// Debug calculator that logs all operations.
    /// Use for comparing calculations with server to identify drift.
    /// </summary>
    public class InstrumentedCalculator : IAmsCalculator
    {
        private readonly IAmsCalculator inner;
        private readonly List<CalculationLog> logs = new();

        public double Epsilon => inner.Epsilon;

        public InstrumentedCalculator() : this(new DefaultCalculator()) { }

        public InstrumentedCalculator(IAmsCalculator inner)
        {
            this.inner = inner ?? new DefaultCalculator();
        }

        public double Add(double a, double b, string context = null)
        {
            var result = inner.Add(a, b, context);
            Log("Add", new[] { a, b }, result, context);
            return result;
        }

        public double Multiply(double a, double b, string context = null)
        {
            var result = inner.Multiply(a, b, context);
            Log("Multiply", new[] { a, b }, result, context);
            return result;
        }

        public double Divide(double a, double b, string context = null)
        {
            var result = inner.Divide(a, b, context);
            Log("Divide", new[] { a, b }, result, context);
            return result;
        }

        public double ApplyPercentage(double baseValue, double percentage, string context = null)
        {
            var result = inner.ApplyPercentage(baseValue, percentage, context);
            Log("ApplyPercentage", new[] { baseValue, percentage }, result, context);
            return result;
        }

        public long ToLong(double value, string context = null)
        {
            var result = inner.ToLong(value, context);
            Log("ToLong", new[] { value }, result, context);
            return result;
        }

        private void Log(string operation, double[] inputs, double output, string context)
        {
            var log = new CalculationLog
            {
                Operation = operation,
                Inputs = inputs,
                Output = output,
                Context = context,
                Timestamp = DateTime.UtcNow
            };
            
            logs.Add(log);
            Logs.AMS?.Debug?.Log(log.ToString());
        }

        public IReadOnlyList<CalculationLog> GetLogs() => logs.AsReadOnly();

        public void ClearLogs() => logs.Clear();

        public string ExportLogsJson()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            for (var i = 0; i < logs.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(Environment.NewLine);
                }
                
                sb.Append(logs[i]);
            }
            sb.Append("]");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Log entry for InstrumentedCalculator.
        /// </summary>
        public struct CalculationLog
        {
            public string Operation;
            public string Context;
            public double[] Inputs;
            public double Output;
            public DateTime Timestamp;

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("{");
                sb.Append($"Operation: {Operation}");
                sb.Append($", Inputs: [{string.Join(", ", Inputs)}]");
                sb.Append($", Output: {Output}");
                sb.Append($", Context: {Context ?? ""}");
                sb.Append($", Timestamp: {Timestamp}");
                sb.Append("}");
                return sb.ToString();
            }
        }
    }
}

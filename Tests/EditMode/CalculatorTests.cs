namespace TeaSpoons.AMS.Tests
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
#if !TEASPOONS_LOGGING
    using System.Text.RegularExpressions;
#endif

    /// <summary>
    /// Runs in projects with and without TeaSpoons.Logging. Without it AMS falls back to the Unity console.
    /// </summary>
    public class CalculatorTests
    {
        [Test]
        public void DefaultCalculator_DoesArithmetic()
        {
            var calc = new DefaultCalculator();
            Assert.AreEqual(5d, calc.Add(2, 3), 1e-9);
            Assert.AreEqual(6d, calc.Multiply(2, 3), 1e-9);
            Assert.AreEqual(2d, calc.Divide(6, 3), 1e-9);
            Assert.AreEqual(110d, calc.ApplyPercentage(100, 10), 1e-9);
        }

        [Test]
        public void InstrumentedCalculator_RecordsEveryOperation()
        {
            var calc = new InstrumentedCalculator();
#if !TEASPOONS_LOGGING
            LogAssert.Expect(LogType.Log, new Regex(@"^\[AMS\] .*Operation: Add"));
#endif
            calc.Add(1, 2);

            Assert.AreEqual(1, calc.GetLogs().Count);
            Assert.AreEqual("Add", calc.GetLogs()[0].Operation);
        }

#if !TEASPOONS_LOGGING
        // The fallback logs to the Unity console (editor and development builds only).
        [Test]
        public void WithoutLogging_DivideByZero_LogsErrorAndThrows()
        {
            var calc = new DefaultCalculator();
            LogAssert.Expect(LogType.Error, new Regex(@"^\[AMS\] Division by zero attempted"));
            Assert.Throws<DivideByZeroException>(() => calc.Divide(1, 0));
        }

        [Test]
        public void WithoutLogging_TryDivideByZero_LogsWarning()
        {
            var calc = new DefaultCalculator();
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[AMS\] TryDivide failed"));
            Assert.IsFalse(calc.TryDivide(1, 0, out _));
        }
#else
        [Test]
        public void WithLogging_DivideByZero_Throws()
        {
            var calc = new DefaultCalculator();
            LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.Throws<DivideByZeroException>(() => calc.Divide(1, 0));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }
#endif
    }
}

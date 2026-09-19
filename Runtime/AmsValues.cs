using System;
using System.Collections.Generic;

namespace TeaSpoons.AMS
{
    /// <summary>
    /// Contains calculated AMS values for a proxy.
    /// Holds permanent values (calculated from containers), modifiers, and current values.
    /// </summary>
    public class AmsValues
    {
        private readonly IAmsCalculator calculator;

        /// <summary>
        /// The raw accumulated ValueSet (before percentage application).
        /// </summary>
        internal readonly ValueSet ValueSet = new();

        /// <summary>
        /// Final calculated permanent values (absolute + percentage applied).
        /// </summary>
        private Dictionary<int, double> permanents = new();

        /// <summary>
        /// Percentage modifiers (for display/debugging). Stored as raw percent values (e.g., 15 means 15%).
        /// </summary>
        private Dictionary<int, double> modifiers = new();
        public IReadOnlyDictionary<int, double> Modifiers => modifiers;

        /// <summary>
        /// Current mutable values (e.g., current HP, current energy).
        /// </summary>
        private readonly Dictionary<int, double> currents = new();
        public IReadOnlyDictionary<int, double> Currents => currents;

        internal AmsValues(IAmsCalculator calculator)
        {
            if (calculator is null)
            {
                Logs.Error("AmsValues ctor; Can't create AmsValues with a null calculator");
                throw new ArgumentNullException(nameof(calculator));
            }
            
            this.calculator = calculator;
        }

        /// <summary>
        /// Recalculates permanent values and modifiers from the ValueSet.
        /// Call this after modifying the ValueSet.
        /// </summary>
        public void Calculate(string context = null)
        {
            permanents = ValueSet.CalculateParameters(calculator);
            modifiers = ValueSet.GetPercentageMapSnapshot();

            Logs.Debug($"AmsValues.Calculate completed. permanents={permanents.Count}, modifiers={modifiers.Count}. Context={context ?? "<null>"}");
        }

        #region Permanents

        /// <summary>
        /// Gets all permanent values.
        /// </summary>
        public IReadOnlyDictionary<int, double> Permanents => permanents;

        /// <summary>
        /// Gets the permanent value for an attribute, or returns <paramref name="fallback"/> if not found.
        /// 
        /// NOTE: This returns a double. Game systems (HP, Speed, etc.) are responsible
        /// for converting to their final type (long/int) using their own truncation rules.
        /// </summary>
        public double GetPermanent(IAmsAttribute attribute, double fallback = 0d, string context = null)
        {
            if (attribute is null)
            {
                Logs.Warning($"GetPermanent: attribute is null, returning fallback value {fallback}");
                return fallback;
            }

            if (permanents.TryGetValue(attribute.Id, out var result))
            {
                return result;
            }

            Logs.Warning($"GetPermanent miss: {attribute.Name} (id={attribute.Id}) => fallback={fallback}. Context={context ?? "<null>"}");

            return fallback;
        }

        /// <summary>
        /// Returns true if a permanent value exists for the given attribute.
        /// </summary>
        public bool TryGetPermanent(IAmsAttribute attribute, out double value)
        {
            if (attribute is not null) return permanents.TryGetValue(attribute.Id, out value);
            
            Logs.Warning("TryGetPermanent: attribute is null, returning default value");
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the permanent value for an attribute or throws if missing.
        /// </summary>
        public double GetPermanentOrThrow(IAmsAttribute attribute, string context = null)
        {
            if (attribute is null)
            {
                Logs.Error("GetPermanentOrThrow: attribute is null.");
                throw new ArgumentNullException(nameof(attribute));
            }

            if (permanents.TryGetValue(attribute.Id, out var result))
            {
                return result;
            }

            Logs.Error(
                $"GetPermanentOrThrow miss: {attribute.Name} (id={attribute.Id}). Context={context ?? "<null>"}");
            throw new KeyNotFoundException(
                $"[AMS] Permanent value not found for attribute '{attribute.Name}' (id={attribute.Id}).");
        }
        
        /// <summary>
        /// Gets the permanent value as a long (using calculator's conversion) for an attribute, or returns <paramref name="fallback"/> if not found.
        /// TRUNCATION POINT: This is where double -> long conversion happens.
        /// </summary>
        public long GetPermanentLong(IAmsAttribute attribute, long fallback = 0L, string context = null)
        {
            if (TryGetPermanent(attribute, out var permanent))
            {
                return calculator.ToLong(permanent, context);
            }

            Logs.Error(attribute is null
                ? $"GetPermanentLong: attribute is null, returning fallback value {fallback}"
                : $"GetPermanentLong: failed to get corresponding value for attribute {attribute.Id}, returning fallback value {fallback}");

            return fallback;
        }

        public long GetPermanentLongOrThrow(IAmsAttribute attribute, string context = null)
        {
            return calculator.ToLong(GetPermanentOrThrow(attribute, context), context);
        }

        #endregion

        #region Modifiers

        /// <summary>
        /// Gets the percentage modifier for an attribute (as a decimal), or returns <paramref name="fallback"/> if not found.
        /// Example: 15% returns 0.15.
        /// </summary>
        public double GetModifier(IAmsAttribute attribute, double fallback = 0d, string context = null)
        {
            if (attribute is null)
            {
                Logs.Warning($"GetModifier: attribute is null, returning fallback value {fallback}");
                return fallback;
            }

            if (modifiers.TryGetValue(attribute.Id, out var rawPercent))
            {
                return rawPercent * 0.01;
            }

            Logs.Warning($"GetModifier miss: {attribute.Name} (id={attribute.Id}) => fallback={fallback}. Context={context ?? "<null>"}");
            return fallback;
        }

        public bool TryGetModifier(IAmsAttribute attribute, out double decimalValue)
        {
            if (attribute is not null) return modifiers.TryGetValue(attribute.Id, out decimalValue);
            
            Logs.Warning("TryGetModifier: attribute is null, returning default value");
            decimalValue = default;
            return false;
        }

        public double GetModifierOrThrow(IAmsAttribute attribute, string context = null)
        {
            if (attribute is null)
            {
                Logs.Error("GetModifierOrThrow: attribute is null.");
                throw new ArgumentNullException(nameof(attribute));
            }

            if (modifiers.TryGetValue(attribute.Id, out var rawPercent))
            {
                return rawPercent * 0.01;
            }

            Logs.Error(
                $"GetModifierOrThrow miss: {attribute.Name} (id={attribute.Id}). Context={context ?? "<null>"}");
            throw new KeyNotFoundException(
                $"[AMS] Modifier not found for attribute '{attribute.Name}' (id={attribute.Id}).");
        }

        /// <summary>
        /// Gets the raw percentage modifier (e.g., 15% returns 15.0), or returns <paramref name="fallback"/> if not found.
        /// </summary>
        public double GetModifierRaw(IAmsAttribute attribute, double fallback = 0d, string context = null)
        {
            if (attribute is null)
            {
                Logs.Warning($"GetModifierRaw: attribute is null, returning fallback value {fallback}");
                return fallback;
            }

            if (modifiers.TryGetValue(attribute.Id, out var rawPercent))
            {
                return rawPercent;
            }

            Logs.Warning($"GetModifierRawOr miss: {attribute.Name} (id={attribute.Id}) => fallback={fallback}. Context={context ?? "<null>"}");
            return fallback;
        }

        public bool TryGetModifierRaw(IAmsAttribute attribute, out double rawPercent)
        {
            if(attribute is not null) return modifiers.TryGetValue(attribute.Id, out rawPercent);
            
            Logs.Warning("TryGetModifierRaw: attribute is null, returning default value");
            rawPercent = default;
            return false;
        }

        public double GetModifierRawOrThrow(IAmsAttribute attribute, string context = null)
        {
            if (attribute is null)
            {
                Logs.Error("GetModifierRawOrThrow: attribute is null.");
                throw new ArgumentNullException(nameof(attribute));
            }

            if (modifiers.TryGetValue(attribute.Id, out var rawPercent))
            {
                return rawPercent;
            }

            Logs.Error(
                $"GetModifierRawOrThrow miss: {attribute.Name} (id={attribute.Id}). Context={context ?? "<null>"}");
            throw new KeyNotFoundException(
                $"[AMS] Raw modifier not found for attribute '{attribute.Name}' (id={attribute.Id}).");
        }

        #endregion

        #region Currents
        /// <summary>
        /// Gets the current value, or returns <paramref name="fallback"/> if not found.
        /// </summary>
        public double GetCurrent(IAmsAttribute attribute, double fallback = 0d, string context = null)
        {
            if (attribute is null)
            {
                Logs.Warning($"GetCurrent: attribute is null, returning fallback value {fallback}");
                return fallback;
            }

            if (currents.TryGetValue(attribute.Id, out var value))
            {
                return value;
            }

            Logs.Warning(
                $"GetCurrentOr miss: {attribute.Name} (id={attribute.Id}) => fallback={fallback}. Context={context ?? "<null>"}");
            return fallback;
        }

        public bool TryGetCurrent(IAmsAttribute attribute, out double value)
        {
            if(attribute is not null) return currents.TryGetValue(attribute.Id, out value);
            
            Logs.Warning("TryGetCurrent: attribute is null, returning default value");
            value = default;
            return false;
        }

        public double GetCurrentOrThrow(IAmsAttribute attribute, string context = null)
        {
            if (attribute is null)
            {
                Logs.Error("GetModifierRawOrThrow: attribute is null.");
                throw new ArgumentNullException(nameof(attribute));
            }

            if (currents.TryGetValue(attribute.Id, out var value))
            {
                return value;
            }

            Logs.Error(
                $"GetCurrentOrThrow miss: {attribute.Name} (id={attribute.Id}). Context={context ?? "<null>"}");
            throw new KeyNotFoundException(
                $"[AMS] Current value not found for attribute '{attribute.Name}' (id={attribute.Id}).");
        }

        public long GetCurrentLong(IAmsAttribute attribute, long fallback = 0L, string context = null)
        {
            var value = GetCurrent(attribute, fallback, context);
            return calculator.ToLong(value, context);
        }

        public long GetCurrentLongOrThrow(IAmsAttribute attribute, string context = null)
        {
            return calculator.ToLong(GetCurrentOrThrow(attribute, context), context);
        }

        /// <summary>
        /// Sets a current value. Returns the value that was set,
        /// returns -1 if attribute didn't find the attribute.
        /// </summary>
        public double SetCurrent(IAmsAttribute attribute, double value, string context = null)
        {
            if (attribute is null)
            {
                Logs.Warning($"SetCurrent: attribute is null.");
                return -1d;
            }

            var existing = GetCurrent(attribute, -1d, context);

            if (Math.Abs(value - existing) <= calculator.Epsilon)
            {
                return value;
            }

            Logs.Debug($"SetCurrent: {attribute.Name} (id={attribute.Id}) = {value} (was {existing}). Context={context ?? "<null>"}");

            currents[attribute.Id] = value;
            return value;
        }

        public void ClearCurrents(string context = null)
        {
            currents.Clear();
            Logs.Debug($"ClearCurrents. Context={context ?? "<null>"}");
        }

        #endregion

        /// <summary>
        /// Checks if two doubles are approximately equal using calculator epsilon.
        /// </summary>
        public bool ApproximatelyEqual(double a, double b)
        {
            return Math.Abs(a - b) < calculator.Epsilon;
        }
    }
}

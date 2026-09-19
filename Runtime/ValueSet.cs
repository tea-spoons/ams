namespace TeaSpoons.AMS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains the four value maps that define an AMS container's contributions.
    /// Handles accumulation and calculation of final attribute values.
    /// </summary>
    public class ValueSet : IEquatable<ValueSet>
    {
        private readonly SortedDictionary<int, double> absoluteMap;
        private readonly SortedDictionary<int, double> percentageMap;
        private readonly SortedDictionary<int, AmsAverageValue> averageAbsoluteMap;
        private readonly SortedDictionary<int, AmsAverageValue> averagePercentageMap;

        // Attribute lookup for debugging/display
        private readonly Dictionary<int, IAmsAttribute> attributeLookup;

        public ValueSet()
        {
            absoluteMap = new SortedDictionary<int, double>();
            percentageMap = new SortedDictionary<int, double>();
            averageAbsoluteMap = new SortedDictionary<int, AmsAverageValue>();
            averagePercentageMap = new SortedDictionary<int, AmsAverageValue>();
            attributeLookup = new Dictionary<int, IAmsAttribute>();
        }

        /// <summary>
        /// Copy constructor - creates a deep copy.
        /// </summary>
        public ValueSet(ValueSet other)
        {
            absoluteMap = new SortedDictionary<int, double>(other.absoluteMap);
            percentageMap = new SortedDictionary<int, double>(other.percentageMap);
            averageAbsoluteMap = new SortedDictionary<int, AmsAverageValue>();
            averagePercentageMap = new SortedDictionary<int, AmsAverageValue>();
            attributeLookup = new Dictionary<int, IAmsAttribute>(other.attributeLookup);

            // Deep copy average values
            foreach (var kvp in other.averageAbsoluteMap)
            {
                averageAbsoluteMap[kvp.Key] = new AmsAverageValue(kvp.Value);
            }
            
            foreach (var kvp in other.averagePercentageMap)
            {
                averagePercentageMap[kvp.Key] = new AmsAverageValue(kvp.Value);
            }
        }

        #region Read Access

        public IReadOnlyDictionary<int, double> AbsoluteMap => absoluteMap;
        public IReadOnlyDictionary<int, double> PercentageMap => percentageMap;
        public IReadOnlyDictionary<int, AmsAverageValue> AverageAbsoluteMap => averageAbsoluteMap;
        public IReadOnlyDictionary<int, AmsAverageValue> AveragePercentageMap => averagePercentageMap;

        public double GetAbsoluteValue(IAmsAttribute attribute)
        {
            return absoluteMap.GetValueOrDefault(attribute.Id, 0.0);
        }

        public double GetPercentageValue(IAmsAttribute attribute)
        {
            return percentageMap.GetValueOrDefault(attribute.Id, 0.0);
        }

        public double GetAverageAbsoluteValue(IAmsAttribute attribute)
        {
            return averageAbsoluteMap.TryGetValue(attribute.Id, out var value) ? value.GetAverage() : 0.0;
        }

        public double GetAveragePercentageValue(IAmsAttribute attribute)
        {
            return averagePercentageMap.TryGetValue(attribute.Id, out var value) ? value.GetAverage() : 0.0;
        }

        public AmsAttributeValue GetAttributeValue(IAmsAttribute attribute)
        {
            return new AmsAttributeValue(
                GetAbsoluteValue(attribute),
                GetPercentageValue(attribute),
                GetAverageAbsoluteValue(attribute),
                GetAveragePercentageValue(attribute)
            );
        }

        public bool IsEmpty()
        {
            return absoluteMap.Count == 0 &&
                   percentageMap.Count == 0 &&
                   averageAbsoluteMap.Count == 0 &&
                   averagePercentageMap.Count == 0;
        }

        #endregion

        #region Write Access

        /// <summary>
        /// Adds an absolute value for an attribute.
        /// </summary>
        public ValueSet AddAbsolute(IAmsAttribute attribute, double magnitude)
        {
            RegisterAttribute(attribute);
            if (absoluteMap.TryGetValue(attribute.Id, out var existing))
            {
                absoluteMap[attribute.Id] = existing + magnitude;
            }
            else
            {
                absoluteMap[attribute.Id] = magnitude;
            }
            return this;
        }

        /// <summary>
        /// Adds a percentage value for an attribute.
        /// </summary>
        public ValueSet AddPercentage(IAmsAttribute attribute, double magnitude)
        {
            RegisterAttribute(attribute);
            if (percentageMap.TryGetValue(attribute.Id, out var existing))
            {
                percentageMap[attribute.Id] = existing + magnitude;
            }
            else
            {
                percentageMap[attribute.Id] = magnitude;
            }
            return this;
        }

        /// <summary>
        /// Adds a weighted average absolute value.
        /// </summary>
        public ValueSet AddAbsoluteAverage(IAmsAttribute attribute, double magnitude, int stackCount)
        {
            RegisterAttribute(attribute);
            if (!averageAbsoluteMap.TryGetValue(attribute.Id, out var avg))
            {
                avg = new AmsAverageValue(magnitude * stackCount, stackCount);
                averageAbsoluteMap[attribute.Id] = avg;
            }
            else
            {
                avg.AddAverage(magnitude, stackCount);
            }
            return this;
        }

        /// <summary>
        /// Adds a weighted average percentage value.
        /// </summary>
        public ValueSet AddPercentageAverage(IAmsAttribute attribute, double magnitude, int stackCount)
        {
            RegisterAttribute(attribute);
            if (!averagePercentageMap.TryGetValue(attribute.Id, out var avg))
            {
                avg = new AmsAverageValue(magnitude * stackCount, stackCount);
                averagePercentageMap[attribute.Id] = avg;
            }
            else
            {
                avg.AddAverage(magnitude, stackCount);
            }

            return this;
        }

        /// <summary>
        /// Adds an AmsValue (parsed from string like "100" or "15%").
        /// </summary>
        public ValueSet AddAmsValue(IAmsAttribute attribute, AmsValue amsValue)
        {
            return amsValue.Type switch
            {
                TransformType.Absolute => AddAbsolute(attribute, amsValue.Magnitude),
                TransformType.Percentage => AddPercentage(attribute, amsValue.Magnitude),
                _ => this
            };
        }

        /// <summary>
        /// Adds all values from another ValueSet, multiplied by stack count.
        /// </summary>
        public void AddValueSet(ValueSet other, int stackCount = 1)
        {
            if (other == null || stackCount <= 0) return;

            // Add absolute values (multiplied by stack)
            foreach (var kvp in other.absoluteMap)
            {
                if (absoluteMap.TryGetValue(kvp.Key, out var existing))
                {
                    absoluteMap[kvp.Key] = existing + kvp.Value * stackCount;
                }
                else
                {
                    absoluteMap[kvp.Key] = kvp.Value * stackCount;
                }
            }

            // Add percentage values (multiplied by stack)
            foreach (var kvp in other.percentageMap)
            {
                if (percentageMap.TryGetValue(kvp.Key, out var existing))
                {
                    percentageMap[kvp.Key] = existing + kvp.Value * stackCount;
                }
                else
                {
                    percentageMap[kvp.Key] = kvp.Value * stackCount;
                }
            }

            // Add average absolute values
            foreach (var kvp in other.averageAbsoluteMap)
            {
                if (!averageAbsoluteMap.TryGetValue(kvp.Key, out var avg))
                {
                    avg = new AmsAverageValue(kvp.Value.TotalValue * stackCount, kvp.Value.StackCount * stackCount);
                    averageAbsoluteMap[kvp.Key] = avg;
                }
                else
                {
                    avg.AddAverage(kvp.Value.TotalValue, kvp.Value.StackCount * stackCount);
                }
            }

            // Add average percentage values
            foreach (var kvp in other.averagePercentageMap)
            {
                if (!averagePercentageMap.TryGetValue(kvp.Key, out var avg))
                {
                    avg = new AmsAverageValue(kvp.Value.TotalValue * stackCount, kvp.Value.StackCount * stackCount);
                    averagePercentageMap[kvp.Key] = avg;
                }
                else
                {
                    avg.AddAverage(kvp.Value.TotalValue, kvp.Value.StackCount * stackCount);
                }
            }

            // Merge attribute lookups
            foreach (var kvp in other.attributeLookup)
            {
                attributeLookup.TryAdd(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Clears all values.
        /// </summary>
        public void Clear()
        {
            absoluteMap.Clear();
            percentageMap.Clear();
            averageAbsoluteMap.Clear();
            averagePercentageMap.Clear();
        }

        private void RegisterAttribute(IAmsAttribute attribute)
        {
            attributeLookup.TryAdd(attribute.Id, attribute);
        }

        #endregion

        #region Calculation

        /// <summary>
        /// Calculates final parameter values using the AMS formula:
        /// result = (sum(absolute) + avg(averageAbsolute)) * (1 + (sum(percentage) + avg(averagePercentage)) * 0.01)
        /// </summary>
        public Dictionary<int, double> CalculateParameters(IAmsCalculator calculator)
        {
            var result = new Dictionary<int, double>();

            // Step 1: Copy absolute values
            foreach (var kvp in absoluteMap)
            {
                result[kvp.Key] = kvp.Value;
            }

            // Step 2: Add average absolute values
            foreach (var kvp in averageAbsoluteMap)
            {
                var avgValue = kvp.Value.GetAverage();
                if (result.TryGetValue(kvp.Key, out var existing))
                {
                    result[kvp.Key] = calculator.Add(existing, avgValue);
                }
                else
                {
                    result[kvp.Key] = avgValue;
                }
            }

            // Step 3: Build combined percentage map (sum percentages + average percentages)
            var combinedPercent = new Dictionary<int, double>(percentageMap);
            foreach (var kvp in averagePercentageMap)
            {
                var avgValue = kvp.Value.GetAverage();
                if (combinedPercent.TryGetValue(kvp.Key, out var existing))
                {
                    combinedPercent[kvp.Key] = calculator.Add(existing, avgValue);
                }
                else
                {
                    combinedPercent[kvp.Key] = avgValue;
                }
            }

            // Step 4: Apply percentages to base values
            foreach (var key in result.Keys.ToList())
            {
                if (combinedPercent.TryGetValue(key, out var percent))
                {
                    result[key] = calculator.ApplyPercentage(result[key], percent);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a snapshot of the raw percentage map (for modifiers display).
        /// </summary>
        public Dictionary<int, double> GetPercentageMapSnapshot()
        {
            var result = new Dictionary<int, double>(percentageMap);

            foreach (var kvp in averagePercentageMap)
            {
                var avgValue = kvp.Value.GetAverage();
                if (result.TryGetValue(kvp.Key, out var existing))
                    result[kvp.Key] = existing + avgValue;
                else
                    result[kvp.Key] = avgValue;
            }

            return result;
        }

        #endregion

        #region Static Factory Methods

        public static ValueSet FromAbsolute(IAmsAttribute attribute, double magnitude)
        {
            var valueSet = new ValueSet();
            valueSet.AddAbsolute(attribute, magnitude);
            return valueSet;
        }

        public static ValueSet FromPercentage(IAmsAttribute attribute, double magnitude)
        {
            var valueSet = new ValueSet();
            valueSet.AddPercentage(attribute, magnitude);
            return valueSet;
        }

        #endregion

        #region Equality

        public bool Equals(ValueSet other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return DictionaryEquals(absoluteMap, other.absoluteMap) &&
                   DictionaryEquals(percentageMap, other.percentageMap) &&
                   AverageDictionaryEquals(averageAbsoluteMap, other.averageAbsoluteMap) &&
                   AverageDictionaryEquals(averagePercentageMap, other.averagePercentageMap);
        }

        private static bool DictionaryEquals(IDictionary<int, double> a, IDictionary<int, double> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var value) || Math.Abs(kvp.Value - value) > AmsCalculatorProvider.Instance.Epsilon)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AverageDictionaryEquals(IDictionary<int, AmsAverageValue> a, IDictionary<int, AmsAverageValue> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as ValueSet);

        public override int GetHashCode()
        {
            return HashCode.Combine(
                absoluteMap.Count,
                percentageMap.Count,
                averageAbsoluteMap.Count,
                averagePercentageMap.Count
            );
        }

        #endregion

        public override string ToString()
        {
            return $"ValueSet [absolute={absoluteMap.Count}, percentage={percentageMap.Count}, " +
                   $"avgAbsolute={averageAbsoluteMap.Count}, avgPercentage={averagePercentageMap.Count}]";
        }
    }
}

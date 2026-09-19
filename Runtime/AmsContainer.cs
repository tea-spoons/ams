namespace TeaSpoons.AMS
{
    using System;

    /// <summary>
    /// Container for AMS values, typically representing an item, buff, or equipment.
    /// </summary>
    public class AmsContainer : IComparable<AmsContainer>, IEquatable<AmsContainer>
    {
        /// <summary>
        /// Unique name within the system.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// System/category this container belongs to (e.g., "Equipment", "Buffs").
        /// </summary>
        public string System { get; }

        /// <summary>
        /// Maximum number of times this container can be stacked.
        /// Null means unlimited stacking.
        /// </summary>
        public short? StackLimit { get; set; }

        /// <summary>
        /// The values this container contributes.
        /// </summary>
        public ValueSet ValueSet { get; set; }

        private int stackCount = 1;

        /// <summary>
        /// Current stack count, clamped to StackLimit if set.
        /// Minimum value is 1 - containers must have at least one stack.
        /// </summary>
        public int StackCount
        {
            get => stackCount;
            set
            {
                // Enforce minimum of 1 - a container must have at least one stack
                var newValue = Math.Max(1, value);

                if (StackLimit.HasValue)
                {
                    newValue = Math.Min(StackLimit.Value, newValue);
                }

                stackCount = newValue;
            }
        }

        public AmsContainer(string name, string system)
            : this(name, system, new ValueSet(), 1, null)
        {
        }

        public AmsContainer(string name, string system, ValueSet valueSet)
            : this(name, system, valueSet, 1, null)
        {
        }

        public AmsContainer(string name, string system, ValueSet valueSet, int stackLimit)
            : this(name, system, valueSet, 1, (short)stackLimit)
        {
        }

        public AmsContainer(string name, string system, ValueSet valueSet, int stackCount, short? stackLimit)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            System = system ?? throw new ArgumentNullException(nameof(system));
            ValueSet = valueSet ?? new ValueSet();
            StackLimit = stackLimit;
            StackCount = stackCount;
        }

        public AmsContainer(AmsContainer other)
            : this(other.Name, other.System, new ValueSet(other.ValueSet), other.StackCount, other.StackLimit)
        {
        }

        
        public bool IsEmpty => ValueSet.IsEmpty();

        public double GetValueAbsolute(IAmsAttribute attribute)
        {
            return ValueSet.GetAbsoluteValue(attribute);
        }

        public double GetValuePercentage(IAmsAttribute attribute)
        {
            return ValueSet.GetPercentageValue(attribute);
        }

        public double GetValueAverageAbsolute(IAmsAttribute attribute)
        {
            return ValueSet.GetAverageAbsoluteValue(attribute);
        }

        public double GetValueAveragePercentage(IAmsAttribute attribute)
        {
            return ValueSet.GetAveragePercentageValue(attribute);
        }

        public AmsAttributeValue GetAttributeValue(IAmsAttribute attribute)
        {
            return ValueSet.GetAttributeValue(attribute);
        }
        
        /// <summary>
        /// Two containers are equal if they have the same Name and System.
        /// Other properties (stack count, values) are state, not identity.
        /// </summary>
        public bool Equals(AmsContainer other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                   string.Equals(System, other.System, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as AmsContainer);

        public override int GetHashCode() => HashCode.Combine(Name, System);

        /// <summary>
        /// Checks if two containers are completely identical (including state).
        /// </summary>
        public bool IsIdentical(AmsContainer other)
        {
            if (!Equals(other)) return false;
            return StackCount == other.StackCount &&
                   Nullable.Equals(StackLimit, other.StackLimit) &&
                   ValueSet.Equals(other.ValueSet);
        }

        /// <summary>
        /// Compares by System, then by Name.
        /// </summary>
        public int CompareTo(AmsContainer other)
        {
            if (other is null) return 1;
            
            var systemCompare = string.Compare(System, other.System, StringComparison.Ordinal);
            
            return systemCompare != 0 ? 
                systemCompare : 
                string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(AmsContainer left, AmsContainer right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(AmsContainer left, AmsContainer right) => !(left == right);

        public override string ToString()
        {
            return $"AmsContainer [name={Name}, system={System}, stackCount={StackCount}, valueSet={ValueSet}]";
        }
    }
}

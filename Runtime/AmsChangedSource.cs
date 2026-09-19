namespace TeaSpoons.AMS
{
    /// <summary>
    /// Source of a change notification in the AMS hierarchy.
    /// </summary>
    public enum AmsChangedSource
    {
        /// <summary>A parent's values changed.</summary>
        Parent,
        /// <summary>A child's values changed.</summary>
        Child,
        /// <summary>This proxy's own values changed (container added/removed).</summary>
        Self
    }
}

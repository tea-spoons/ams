namespace TeaSpoons.AMS
{
    /// <summary>
    /// Interface for AMS attribute identifiers.
    /// Implement this on an enum wrapper to define your game's attributes.
    /// </summary>
    /// <example>
    /// public class GameAttr : IAmsAttribute
    /// {
    ///     private readonly GameAttribute _attr;
    ///     public GameAttr(GameAttribute attr) => _attr = attr;
    ///     public int Id => (int)_attr;
    ///     public string Name => _attr.ToString();
    /// }
    /// </example>
    public interface IAmsAttribute
    {
        /// <summary>
        /// Unique identifier for the attribute.
        /// For enums, this is typically the underlying integer value.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Display name for debugging and logging.
        /// </summary>
        string Name { get; }
    }
}

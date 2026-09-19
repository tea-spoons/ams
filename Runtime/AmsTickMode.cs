namespace TeaSpoons.AMS
{
    /// <summary>
    /// Determines when AMS proxies are ticked (recalculated).
    /// </summary>
    public enum AmsTickMode
    {
        /// <summary>
        /// No automatic ticking. Call AmsProxy.Tick() manually.
        /// </summary>
        Manual,

        /// <summary>
        /// Tick during Unity's Update loop.
        /// </summary>
        Update,

        /// <summary>
        /// Tick during Unity's LateUpdate loop.
        /// Best for ensuring all game logic has run before recalculating.
        /// </summary>
        LateUpdate,

        /// <summary>
        /// Tick during Unity's FixedUpdate loop.
        /// Use for physics-related attributes.
        /// </summary>
        FixedUpdate
    }
}

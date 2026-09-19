namespace TeaSpoons.AMS
{
#if TEASPOONS_LOGGING
    using Logging;
#else
    using System.Diagnostics;
#endif

    /// <summary>
    /// Internal logging front. Uses TeaSpoons.Logging when that package is in the project (TEASPOONS_LOGGING),
    /// otherwise falls back to the Unity console.
    /// </summary>
#if UNITY_EDITOR && TEASPOONS_LOGGING
    [UnityEditor.InitializeOnLoad]
#endif
    internal static class Logs
    {
#if TEASPOONS_LOGGING
        public static readonly LogCategory AMS = new LogCategory("AMS");

        public static void Debug(string message) => AMS?.Debug?.Log(message);

        public static void Warning(string message) => AMS?.Warning?.Log(message);

        public static void Error(string message) => AMS?.Error?.Log(message);
#else
        // The fallback writes to the Unity console, in the editor and in development builds only.
        // The compiler removes calls to a [Conditional] method, arguments included, when none of the symbols is defined,
        // so in release builds these calls cost nothing.
        private const string Prefix = "[AMS] ";

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(string message) => UnityEngine.Debug.Log(Prefix + message);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(string message) => UnityEngine.Debug.LogWarning(Prefix + message);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Error(string message) => UnityEngine.Debug.LogError(Prefix + message);
#endif
    }
}

namespace TeaSpoons.AMS
{
    using Logging;
    
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    internal static class Logs
    {
        public static readonly LogCategory AMS  = new LogCategory("AMS");
    }
}

using UnityEngine;
using GameSystem;

public static class CustomLog
{
    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    // ReSharper disable Unity.PerformanceAnalysis
    public static void Log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
        LogConsole.instance.Log(message);
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
        LogConsole.instance.Log(message, Color.red);
    }

        public static void LogWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#endif
        LogConsole.instance.Log(message, Color.yellow);
    }
}
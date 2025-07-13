using UnityEngine;
using GameSystem;

public static class CustomLog
{
    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    // ReSharper disable Unity.PerformanceAnalysis
    public static void Log(object message)
    {
#if DEBUG
        Debug.Log(message);
#endif
        LogConsole.instance.Log(message);
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogError(object message)
    {
#if DEBUG
        Debug.LogError(message);
#endif
        LogConsole.instance.Log(message, Color.red);
    }

    public static void LogWarning(object message)
    {
#if DEBUG
        Debug.LogWarning(message);
#endif
        LogConsole.instance.Log(message, Color.yellow);
    }

        public static void UnityLog(object message, bool isError = true)
        {
#if DEBUG
                if (isError)
                        Debug.LogError(message);
                else
                        Debug.Log(message);
#endif
        }
}
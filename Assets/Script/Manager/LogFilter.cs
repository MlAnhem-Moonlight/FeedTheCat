using UnityEngine;

/// <summary>
/// Centralized log control. Disables Unity's global logger by default and exposes
/// a controlled logging method for level-data transfer diagnostics only.
/// </summary>
public static class LogFilter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Disable Unity's global logger so existing Debug.* calls produce no output by default.
        if (Debug.unityLogger != null)
            Debug.unityLogger.logEnabled = false;
    }

    private static void WithTemporaryLogging(System.Action action)
    {
        if (Debug.unityLogger == null)
        {
            action?.Invoke();
            return;
        }

        bool prev = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = true;
        try
        {
            action?.Invoke();
        }
        finally
        {
            Debug.unityLogger.logEnabled = prev;
        }
    }

    /// <summary>
    /// Logs messages specifically about level-data transfer. Other Debug.Log calls remain muted.
    /// </summary>
    public static void LogLevelTransfer(string message)
    {
        WithTemporaryLogging(() => Debug.Log(message));
    }

    public static void LogLevelTransferWarning(string message)
    {
        WithTemporaryLogging(() => Debug.LogWarning(message));
    }

    public static void LogLevelTransferError(string message)
    {
        WithTemporaryLogging(() => Debug.LogError(message));
    }

    public static void LogObject(object message)
    {
        WithTemporaryLogging(() => Debug.Log(message));
    }

    // Item-specific logging helpers (useful for debugging item drag/apply flows)
    public static void LogItem(string message)
    {
        WithTemporaryLogging(() => Debug.Log(message));
    }

    public static void LogItemWarning(string message)
    {
        WithTemporaryLogging(() => Debug.LogWarning(message));
    }

    public static void LogItemError(string message)
    {
        WithTemporaryLogging(() => Debug.LogError(message));
    }
}

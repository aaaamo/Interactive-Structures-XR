using UnityEngine;

/// <summary>
/// Filters out spammy Unity log messages at runtime.
/// Attach this to a GameObject in the scene or use [RuntimeInitializeOnLoadMethod].
/// </summary>
public class LogFilter : MonoBehaviour
{
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        Application.logMessageReceived += FilterLogMessage;
        Debug.Log("[LogFilter] Log filtering enabled");
    }

    static void FilterLogMessage(string logString, string stackTrace, LogType type)
    {
        // These messages are handled by our filter - we suppress them by doing nothing
        // The actual suppression happens in the custom log handler below
    }

    void Awake()
    {
        // Set custom log handler to filter messages
        Debug.unityLogger.logHandler = new FilteredLogHandler(Debug.unityLogger.logHandler);
    }

    private class FilteredLogHandler : ILogHandler
    {
        private ILogHandler defaultHandler;

        // Messages to suppress (contains check)
        private static readonly string[] suppressedMessages = new string[]
        {
            "render graph API, the output Render Texture must have a depth buffer",
            "Depth Stencil Format property of the texture must be set"
        };

        public FilteredLogHandler(ILogHandler defaultHandler)
        {
            this.defaultHandler = defaultHandler;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            string message = string.Format(format, args);

            // Check if this message should be suppressed
            foreach (string suppressed in suppressedMessages)
            {
                if (message.Contains(suppressed))
                {
                    return; // Suppress this message
                }
            }

            defaultHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(System.Exception exception, UnityEngine.Object context)
        {
            defaultHandler.LogException(exception, context);
        }
    }
}

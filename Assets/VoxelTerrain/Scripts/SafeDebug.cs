using System;
using UnityEngine;
using System.Collections;

public static class SafeDebug {

    public static void Log(object message) {
        string stackTrace = StackTraceUtility.ExtractStackTrace();
        Loom.QueueMessage(Loom.messageType.Log, message.ToString() + "\n" + stackTrace);
    }

    public static void LogWarning(object message) {
        string stackTrace = StackTraceUtility.ExtractStackTrace();
        Loom.QueueMessage(Loom.messageType.Warning, message.ToString() + "\n" + stackTrace);
    }

    public static void LogError(object message, Exception e = null) {
        string stackTrace = StackTraceUtility.ExtractStackTrace();
        string ErrorLocation = string.Empty;
#if UNITY_EDITOR
        if (e != null)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(e, true);
            System.Diagnostics.StackFrame frame = trace.GetFrame(0);
            ErrorLocation = "\n" + frame.GetFileName() + "." + frame.GetMethod() + ": " + frame.GetFileLineNumber();
        }
#endif
        Loom.QueueMessage(Loom.messageType.Error, message.ToString() + ErrorLocation + "\n" + stackTrace);
    }

    public static void LogException(System.Exception message) {
        Loom.QueueOnMainThread(() => {
            Debug.LogException(message);
        });
    }
}

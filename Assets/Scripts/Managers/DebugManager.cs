using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = true;
    public static DebugManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Log(bool innerEnableDebugLogs, string message)
    {
        if (Instance == null
         || !Instance.enableDebugLogs
          || !innerEnableDebugLogs) return;

        Debug.Log(message);
    }

    public static void LogWarning(bool innerEnableDebugLogs, string message)
    {
        if (Instance == null
         || !Instance.enableDebugLogs
          || !innerEnableDebugLogs) return;

        Debug.LogWarning(message);
    }

    public static void LogError(bool innerEnableDebugLogs, string message)
    {
        if (Instance == null
         || !Instance.enableDebugLogs
          || !innerEnableDebugLogs) return;

        Debug.LogError(message);
    }
}

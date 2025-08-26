using UnityEngine;

// 调试管理器，管理调试相关的状态和业务逻辑
public class DebugManager
{
    private static DebugManager _instance;
    public static DebugManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new DebugManager();
            return _instance;
        }
    }

    private bool _enableUIPathPrint = false;
    public bool IsUIPathPrintEnabled => _enableUIPathPrint;
} 
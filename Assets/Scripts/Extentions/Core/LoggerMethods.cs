using UnityEngine;

namespace Assets.Scripts.Extentions
{
    public static class LoggerMethods
    {
        private const string TAG = "Logger Methods: ";
#if UNITY_EDITOR || true
        private static bool bDebug = true;
#else
        private static bool bDebug = false;
#endif

        public static void Log(string info)
        {
            if (bDebug)
                GameManager.print(string.Format("{0}{1}", TAG, info));
        }

        public static void Log(string info, Object obj)
        {
            if (bDebug)
                Debug.Log(string.Format("{0}{1}", TAG, info), obj);
        }

        public static void LogWarning(string info, Object obj)
        {
            if (bDebug)
                Debug.LogWarning(string.Format("{0}{1}", TAG, info), obj);
        }

        public static void LogWarning(string info)
        {
            if (bDebug)
                Debug.LogWarning(string.Format("{0}{1}", TAG, info));
        }

        public static void LogError(string info)
        {
            if (bDebug)
                Debug.LogError(string.Format("{0}{1}", TAG, info));
        }

        public static void LogError(string info, Object obj)
        {
            if (bDebug)
                Debug.LogError(string.Format("{0}{1}", TAG, info), obj);
        }

        public static void LogException(System.Exception info)
        {
            if (bDebug)
                Debug.LogException(info);
        }

        public static void LogException(System.Exception info, Object obj)
        {
            if (bDebug)
                Debug.LogException(info, obj);
        }
    }
}

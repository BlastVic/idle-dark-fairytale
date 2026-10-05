using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Extentions
{
    public class ThreadRouter : MonoBehaviour
    {
        private static readonly List<Action> _unityThreadReadyCallbacks = new List<Action>();

        private void Update()
        {
            foreach (var callback in _unityThreadReadyCallbacks)
            {
                callback.SafeInvoke();
            }
            _unityThreadReadyCallbacks.Clear();
        }

        public static void RouteCallbackToUnityThread(Action callback)
        {
            _unityThreadReadyCallbacks.Add(callback);
        }
    }
}
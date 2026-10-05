using UnityEngine;

namespace Assets.Scripts.Extentions.ResourceAsset.Singletons
{
    public abstract class StaticMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var objects = FindObjectsOfType<T>();
                    if (objects.Length > 0)
                    {
                        foreach (var obj in objects)
                        {
                            if (_instance == null && obj != null)
                            {
                                _instance = objects[0];
                            }
                            else
                            {
                                Destroy(obj);
                            }
                        }
                    }
                    else
                    {
                        var instanceHolder = new GameObject(typeof(T).Name);
                        _instance = instanceHolder.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        private static Transform GetRootTransform(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return null;
            }

            while (targetTransform.parent != null)
            {
                targetTransform = targetTransform.parent;
            }

            return targetTransform;
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(GetRootTransform(transform));
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }

        protected virtual void OnDestroy()
        {
        }
    }
}
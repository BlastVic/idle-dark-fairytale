using System;
using UnityEngine;

// Entry point retained for settings and the keyboard shortcut. All visuals live in a UGUI prefab.
public class DebugMenu : MonoBehaviour
{
    static DebugMenu instance;
    public static DebugMenu Instance => instance ? instance : FindFirstObjectByType<DebugMenu>();
    public DeveloperOverlayUI overlayPrefab;
    public DeveloperOverlayUI View { get; private set; }
    public bool m_ShowDebug => View && View.gameObject.activeSelf;
    Action onClose;
    void Awake(){if(instance&&instance!=this){Destroy(gameObject);return;}instance=this;DontDestroyOnLoad(gameObject);}
    public void Open(Action callback=null){if(!overlayPrefab)return;if(!View){View=Instantiate(overlayPrefab);DontDestroyOnLoad(View.gameObject);View.owner=this;}onClose=callback;View.gameObject.SetActive(true);View.SetExpanded(true);}
    public void Close(){if(View)View.gameObject.SetActive(false);var callback=onClose;onClose=null;callback?.Invoke();}
    void Update(){if(Input.GetKeyDown(KeyCode.LeftShift)){if(m_ShowDebug)Close();else Open();}}
    void OnDestroy(){if(instance!=this)return;if(View)Destroy(View.gameObject);instance=null;}
}

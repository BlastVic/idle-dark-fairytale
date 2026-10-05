using UnityEngine;
using System.Collections;

public class DisableDestroyOnTimer : MonoBehaviour {
    public enum Options { destroy, deactivate }
    public Options options;
    public float timer;

    private void OnEnable()
    {
        Invoke("delayedAction", timer);
    }

    public void Init()
    {
       // Debug.Log("Awakened:" + gameObject.name);
        Invoke("delayedAction", timer);
    }

    void delayedAction()
    {
        if (options == Options.deactivate)
        {
          //  Debug.Log("deactivated:" + gameObject.name);
            gameObject.SetActive(false);
        }
        if (options == Options.destroy) Destroy(gameObject);
    }
}

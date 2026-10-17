using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotifierText : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;

        GetComponent<MenuFader>().FadeOutCustom(Random.Range(.005f, .01f));
        //transform.position += Random.Range(-3f, 3f);
    }

    private void FixedUpdate()
    {
        transform.position += new Vector3(0, .06f, 0);
    }

}

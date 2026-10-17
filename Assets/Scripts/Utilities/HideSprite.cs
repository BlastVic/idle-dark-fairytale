using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideSprite : MonoBehaviour
{
    private void OnEnable()
    {
        GetComponent<SpriteRenderer>().enabled = false;
    }

}

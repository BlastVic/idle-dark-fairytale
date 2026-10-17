using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeparentOnEnable : MonoBehaviour
{
    private void OnEnable()
    {
        transform.SetParent(null);
    }
}

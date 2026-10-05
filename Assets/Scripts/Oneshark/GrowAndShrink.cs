using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrowAndShrink : MonoBehaviour
{
    public float targetScale = 1;
    float scalingTime, scalingSpeed = 0.05f;
    float length = 0.1f;

    void FixedUpdate()
    {
        scalingTime = Time.time * scalingSpeed;
        transform.localScale = new Vector3(
            Mathf.PingPong(scalingTime, length) + targetScale,
            Mathf.PingPong(scalingTime, length) + targetScale, 0
        );
    }
}
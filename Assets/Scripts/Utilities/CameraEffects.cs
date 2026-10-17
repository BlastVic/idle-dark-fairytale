using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    private static CameraEffects _instance;
    public static CameraEffects single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<CameraEffects>();
            return _instance;
        }
    }

    private void Start()
    {
        originalPosition = transform.position;
    }

    public IEnumerator Shake(float duration = .15f, float magnitude = .07f)
    {
        //Vector3 originalPos = transform.localPosition;

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1, 1) * magnitude;
            float y = Random.Range(-1, 1) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPosition.z);
            
            elapsed += Time.deltaTime;

            yield return null;
        }

        SetOriginalCameraPosition();
    }

    public Vector3 originalPosition;
    public Vector3[] cameraPositions;
    public void SetCameraPosition(int i)
    {
        transform.position = cameraPositions[i];
    }

    public void SetOriginalCameraPosition()
    {
        transform.position = originalPosition;
    }
}

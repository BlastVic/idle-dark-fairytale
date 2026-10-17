using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomScaleOnEnable : MonoBehaviour
{
    public float minSize = 2;
    public float maxSize = 7;

    private void OnEnable()
    {
        float sizeChosen = Random.Range(minSize, maxSize);
        gameObject.transform.localScale = new Vector3(sizeChosen, sizeChosen, sizeChosen);
    }

}

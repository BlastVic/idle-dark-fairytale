using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWASD : MonoBehaviour
{
    public float moveSpeed = 50;
    public Transform obj;

    private void OnEnable()
    {
        obj = GetComponent<Transform>();
    }

    public void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 tempVect = new Vector3(h, v, 0);
        tempVect = tempVect.normalized * moveSpeed * Time.deltaTime;

        obj.transform.position += tempVect;
    }
}

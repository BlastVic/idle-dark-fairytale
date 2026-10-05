using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickDrag : MonoBehaviour
{
    public Vector3 screenPoint;
    public bool tapDown = false;
    public Camera cam;
    public Vector3 lastMousePosition;
    public Vector3 diff;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 curPosition = cam.ScreenToWorldPoint(curScreenPoint);
            curPosition.z = transform.position.z;
            lastMousePosition = curPosition;//reset the last mouse pos
            tapDown = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            tapDown = false;
            MapPackageUI.single.isDragging = false;
        }
        if (tapDown) OnFingerDown();
    }

    private void FixedUpdate()
    {

    }
    void OnFingerDown()
    {
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z);
        Vector3 curPosition = cam.ScreenToWorldPoint(curScreenPoint);
        curPosition.z = transform.position.z;

        diff = curPosition - lastMousePosition;
        diff.x = 0;


        if (diff.y != 0)
        {
            MapPackageUI.single.isDragging = true;
        }

        lastMousePosition = curPosition;

        transform.position += diff;

        float safeY = Mathf.Max(MapPackageUI.MAP_MIN_Y, transform.position.y);
        safeY = Mathf.Min(MapPackageUI.MAP_MAX_Y, safeY);
        transform.position = new Vector3(transform.position.x, safeY, transform.position.z);
    }
}


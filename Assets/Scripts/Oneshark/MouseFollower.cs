using UnityEngine;
using System.Collections;

public class MouseFollower : MonoBehaviour
{

    public bool m_IsCameraSpace = false;
    public Vector3 offset = new Vector3(0, 0, 0);
    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_IsCameraSpace)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, Input.mousePosition+offset, Camera.main, out pos);
            transform.position = transform.TransformPoint(pos);
        }
        else
        {
            transform.position = Input.mousePosition + offset;

        }
    }
}

using UnityEngine;
using System.Collections;

public class SmoothFollow : MonoBehaviour
{
    public GameObject target;
    public float smoothTime = 0.3F;
    private Vector3 velocity = Vector3.zero;
    public Vector3 offset = new Vector3(0, 0, -10);
    public float minZLocation;
    public float maxZLocation;
    public bool m_UseClampX = false;
    public float minXLocation = -10;
    public float maxXLocation=10;
    void FixedUpdate()
    {
        if (target)
        {
            // Smoothly move the camera towards that target position
            #region Z clamping
            float zClamp = Mathf.Clamp(target.transform.position.z+offset.z, minZLocation, maxZLocation);
            Vector3 newPos = new Vector3(target.transform.position.x + offset.x, target.transform.position.y + offset.y, zClamp);
            #endregion

            #region X clamping
            if (m_UseClampX)
            {
                float xClamp = Mathf.Clamp(target.transform.position.x + offset.x, minXLocation, maxXLocation);
                newPos = new Vector3(xClamp, target.transform.position.y + offset.y, zClamp);
                #endregion
            }
            // transform.position = Vector3.SmoothDamp(transform.position, target.transform.position+offset,ref velocity, smoothTime);
            transform.position = Vector3.SmoothDamp(transform.position, newPos, ref velocity, smoothTime);
        }
    }
}

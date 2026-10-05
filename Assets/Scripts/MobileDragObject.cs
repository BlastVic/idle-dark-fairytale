using UnityEngine;

public class MobileDragObject : MonoBehaviour
{
    public float dragSpeed = 2;
    private Vector3 dragOrigin;

    public float m_Bottom = -40f;
    public float m_Left = -4;
    public float m_Top = 10.75f;
    public float m_Right = 95;

    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(0)) return;

        if (Camera.main == null) return;//protection from rare issue
        Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
        Vector3 move = new Vector3(pos.x * -dragSpeed, pos.y * -dragSpeed, 0);

        transform.Translate(move, Space.World);



        Vector3 clampedPosition = transform.position;
        if (clampedPosition.x > m_Right) clampedPosition.x = m_Right;
        if (clampedPosition.x < m_Left) clampedPosition.x = m_Left;
        if (clampedPosition.y > m_Top) clampedPosition.y = m_Top;
        if (clampedPosition.y < m_Bottom) clampedPosition.y = m_Bottom;

        transform.position = clampedPosition;
    }


}
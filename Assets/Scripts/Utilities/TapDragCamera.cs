using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TapDragCamera : MonoBehaviour
{

    // Use this for initialization
    //void Start () {
    //       m_LastPos = Input.mousePosition;
    //}

    private void Awake()
    {
        //This just makes map match mouse current position with no "jumps"
        Invoke("SetMouse", .1f);
    }

    void SetMouse()
    {
        // m_LastPos = Input.mousePosition;
        m_MouseDown = false;

    }

    bool MouseOverGameObject()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }


    public bool m_MouseDown;

    Vector3 m_LastPos = Vector3.zero;
    Vector3 m_FrameDelta = Vector3.zero;
    public float m_Bottom = -40f;
    public float m_Left = -4;
    public float m_Top = 10.75f;
    public float m_Right = 95;
    private void Update()
    {
        if (Input.GetMouseButton(0))
        {

            m_MouseDown = true;
            if (MouseOverGameObject()) m_MouseDown = false;
        }
        if (Input.GetMouseButtonUp(0))
        {
            m_MouseDown = false;
        }

        if (m_MouseDown)
        {
            m_FrameDelta = m_LastPos - Input.mousePosition;
            transform.position += (m_FrameDelta * (Time.deltaTime*.5f));
            //CLAMP
            Vector3 clampedPosition = transform.position;
            if (clampedPosition.x > m_Right) clampedPosition.x = m_Right;
            if (clampedPosition.x < m_Left) clampedPosition.x = m_Left;
            if (clampedPosition.y > m_Top) clampedPosition.y = m_Top;
            if (clampedPosition.y < m_Bottom) clampedPosition.y = m_Bottom;

            transform.position = clampedPosition;
            //Debug.Log("Delta:" + m_FrameDelta);
        }
        m_LastPos = Input.mousePosition;

    }
}

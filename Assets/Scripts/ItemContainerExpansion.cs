using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemContainerExpansion : MonoBehaviour
{
    public float screenWidth = 828;
    public float childrenWidth = 200;
    public float paddingLeftOfContentRect = 0;

    public float expandFactor = .2f;
    //public float contentRectRight = 0;
    RectTransform rt;
    float childCount = 0;

    public bool overrideCalc = false;

    void CalcExpansionFactor()
    {
        if (overrideCalc) return;
        switch (Screen.width)
        {
            case 750:
                expandFactor = .16f;
                break;
            case 828:
                expandFactor = .17f;
                break;
            case 1125:
                expandFactor = .2f;
                break;
            case 1242:
                expandFactor = .23f;
                break;
        }
    }

    public void ExpandContanerWidth()
    {
        return;
        CalcExpansionFactor();
        rt = GetComponent<RectTransform>();
        screenWidth = Screen.width;
        childrenWidth = screenWidth * expandFactor;
        //Debug.Log("screen width:" + screenWidth);
        childCount = transform.childCount;
        float distExpanded = screenWidth - (childrenWidth * childCount);
        distExpanded -= paddingLeftOfContentRect * 2; //add padding to the beginning, so item 1 can reach center

        //Debug.Log("value:" + distExpanded);
        rt.offsetMax = new Vector2(-distExpanded, rt.offsetMax.y);
        rt.anchoredPosition = new Vector3(500, rt.anchoredPosition.y, 0);
    }
}

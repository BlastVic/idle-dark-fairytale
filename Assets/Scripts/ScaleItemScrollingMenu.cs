using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleItemScrollingMenu : MonoBehaviour
{
    public float scaleNorm = 1;
    public float scaleExpanded = 1.3f;
    RectTransform rt;
    GameObject menuCenter;
    public float scaleDistance = 50;
    //public CanvasGroup cgText;
    CanvasGroup cg;
    private void Start()
    {
        menuCenter = GameObject.FindGameObjectWithTag("MenuCenter").gameObject;
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        CheckDistance();
    }

    bool wasChosen = false;
    void CheckDistance()
    {
        if (Vector3.Distance(rt.transform.position, menuCenter.transform.position) < scaleDistance)
        {
            targetScale = scaleExpanded;
            cg.alpha = 1;
            //cgText.alpha = 1;
            wasChosen = true;
            GetComponent<InventoryMenuItem>().SetMeAsChosen();
        }
        else
        {
            if (wasChosen)
            {
                wasChosen = false;
                InventoryUI.single.ClearHighlightedItem();//try to call this once, then the next item up will set itself as chosen
            }
            targetScale = 1;
            cg.alpha = .7f;
            //cgText.alpha = 0;
        }
    }

    public float targetScale = 1;
    public float scalingSpeed = 0.05f;

    void FixedUpdate()
    {
        float current = transform.localScale.x;
        if (current != targetScale)
        {
            //need to shrink
            float nextScale = Mathf.MoveTowards(current, targetScale, scalingSpeed);
            transform.localScale = new Vector3(nextScale, nextScale, nextScale);
        }


    }
}

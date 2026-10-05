using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionCanvas : MonoBehaviour
{
    private static TransitionCanvas _instance;
    public static TransitionCanvas single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<TransitionCanvas>();
            return _instance;
        }
    }

    public Animator anim;
    public bool isBlack = true;
    public void ToBlack(string from = "toShow")
    {
        //Debug.Log("From:" + from);
        if (isBlack)
        {
            Debug.Log("Double IsBlack call on the Trans Canvas");
            return;
        }
        //Debug.Log("ToBlack");
        anim.CrossFade("ToBlack", 0);
        isBlack = true;
    }

    public void ToClear(string from = "toClear")
    {
        //Debug.Log("ToClear From:" + from);
        if (!isBlack)
        {
            Debug.Log("Double ToClear call on the Trans Canvas");
            return;
        }
        //Debug.Log("ToClear");
        anim.CrossFade("ToClear", 0);
        isBlack = false;
    }

    public void ForceClear()
    {
        isBlack = true;
        Debug.Log("Forced Clear");
        anim.CrossFade("ToClear", 0);
        isBlack = false;
    }
}

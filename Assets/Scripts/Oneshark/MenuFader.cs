using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFader : MonoBehaviour
{
    public float fadeSpeed = .2f;
    public bool fadeInOnEnable = true;
    CanvasGroup cg;

    private void OnEnable()
    {
        cg =  GetComponent<CanvasGroup>();
        if (fadeInOnEnable) FadeIn();
    }

    //public void FadeOut()
    //{
    //    if (cg.alpha == 0) return;//no work to do - we are already fully visible

    //    StartCoroutine(FadeOutRoutine());
    //}

    private IEnumerator FadeOutRoutine()
    {
        float startVal = 1;
        CanvasGroup cg = GetComponent<CanvasGroup>();

        while (cg.alpha > 0)
        {
            cg.alpha -= fadeSpeed;
            yield return null;
        }
    }

    public void FadeOutCustom(float _speed)
    {
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (cg.alpha == 0) return;//no work to do - we are already fully visible

        StartCoroutine(FadeOutCustomRoutine(_speed));
    }

    private IEnumerator FadeOutCustomRoutine(float _speed)
    {
        fadeSpeed = _speed;
        float startVal = 1;

        while (cg.alpha > 0)
        {
            cg.alpha -= _speed;
            yield return null;
        }
    }

    public void FadeIn()
    {
        cg.alpha = 0;//start invisible
        cg.interactable = true;
        cg.blocksRaycasts = true;
        if (cg.alpha == 1) return;//no work to do - we are already fully visible
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float startVal = 0;

        while (cg.alpha < 1)
        {
            cg.alpha += fadeSpeed;
            yield return null;
        }
    }
}

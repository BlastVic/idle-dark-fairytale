using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MeterMover : MonoBehaviour
{
    public bool m_IncludeColorAlphaLerp;
    public Image m_ColoredImage;
    public Color m_Color;
    public float m_MovementSpeed = .8f;
    public void SetValue(Image imageToFill, float desiredValue, bool smooth = true)
    {
        if (imageToFill == null) imageToFill = GetComponent<Image>();//send a null if you want
                                                                     // Debug.Log("image was:" + imageToFill);
        StopAllCoroutines();

        if (!smooth && isActiveAndEnabled)
        {
            imageToFill.fillAmount = desiredValue;
            return;
        }

        if (isActiveAndEnabled)
            StartCoroutine(LerpFilledImage(imageToFill, desiredValue));
    }

    public bool isLerping;
    public IEnumerator LerpFilledImage(Image imageToFill, float desiredValue)
    {
        //Debug.Log("lerpFilledImage");
        //StartCoroutine(LerpFilledImage(m_JadsMeter, 1, .1f));
        float increment = 0;
        isLerping = true;
        float startValue = imageToFill.fillAmount;
        if (desiredValue < startValue)
        {
            //going down
            increment = -m_MovementSpeed;
            while (imageToFill.fillAmount > desiredValue)
            {
                imageToFill.fillAmount += increment * Time.deltaTime;
                if (m_IncludeColorAlphaLerp)
                {
                    m_Color.a = increment * Time.deltaTime;
                    m_ColoredImage.color = m_Color;
                }
                yield return null;
                if (!isLerping) break;
            }
        }
        else
        {
            //going up
            increment = m_MovementSpeed;
            while (imageToFill.fillAmount < desiredValue)
            {
                imageToFill.fillAmount += increment * Time.deltaTime;
                if (m_IncludeColorAlphaLerp)
                {
                    m_Color.a += increment * Time.deltaTime;
                    m_ColoredImage.color = m_Color;
                }
                yield return null;
                if (!isLerping) break;
            }
        }

        //we were close enough now set it precise
        if (m_IncludeColorAlphaLerp)
        {
            m_Color.a = desiredValue;
            m_ColoredImage.color = m_Color;
        }
        //Debug.Log("desiredValue:" + desiredValue);
        imageToFill.fillAmount = desiredValue;
    }
}

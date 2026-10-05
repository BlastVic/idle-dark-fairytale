using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpText : MonoBehaviour
{
    public Color green, petColor, lightningColor;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;

        GetComponent<MenuFader>().FadeOutCustom(Random.Range(.005f, .01f));
        //transform.position += Random.Range(-3f, 3f);
    }

    private void FixedUpdate()
    {
        transform.position += new Vector3(0, .08f, 0);
    }

    public GameObject whiteText, redText, whiteCritText, redCritText, whiteCritWord, redCritWord;

    public void TurnPurple()
    {
        whiteText.GetComponent<Text>().color = petColor;

    }

    public void TurnBlue()
    {
        whiteText.GetComponent<Text>().color = lightningColor;

    }
    public void SetMe(bool isPlayer, bool isCrit, float val, bool isHeal = false)
    {
        whiteText.SetActive(false);
        redText.SetActive(false);
        whiteCritText.SetActive(false);
        redCritText.SetActive(false);
        whiteCritWord.SetActive(false);
        redCritWord.SetActive(false);
        if (isPlayer)
        {
            if (isCrit)
            {
                redCritWord.SetActive(true);
                redCritText.SetActive(true);
                redCritText.GetComponent<Text>().text = Utilities.ConvertNumber(val);
            }
            else
            {
                redText.SetActive(true);
                redText.GetComponent<Text>().text = Utilities.ConvertNumber(val);
            }
        }
        else
        {
            if (isCrit)
            {
                whiteCritWord.SetActive(true);
                whiteCritText.SetActive(true);
                whiteCritText.GetComponent<Text>().text = Utilities.ConvertNumber(val);
            }
            else
            {
                whiteText.SetActive(true);
                whiteText.GetComponent<Text>().text = Utilities.ConvertNumber(val);
            }
        }
    }
}

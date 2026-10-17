using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MathTest : MonoBehaviour
{
    public Text result;

    public float numberIn;
    // Start is called before the first frame update
    public void Calculate()
    {
        //result.text = Mathf.Round(numberIn).ToString();
        //result.text= (numberIn % 1f).ToString();
        result.text = Mathf.Round(numberIn).ToString();
        //result.text = Utilities.ConvertNumber(numberIn);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPopUpAnimation : MonoBehaviour
{
  public void AnimationComplete()
    {
        GameplayCanvas.single.ItemPopUpEnded();
    }
}

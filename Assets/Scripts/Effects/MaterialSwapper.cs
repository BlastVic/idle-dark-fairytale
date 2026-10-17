using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSwapper : MonoBehaviour
{
    public SkeletonAnimation skeletonAnimation;

    private void OnEnable()
    {
        if (skeletonAnimation == null) GetComponent<SkeletonAnimation>();
    }

    public void CallWhiteFlash()
    {
        WhiteFlash();
    }

    public Material[] whiteMats;
    public List<Material> origMats;
    public void WhiteFlash()
    {
        for (int i = 0; i < whiteMats.Length; i++)
        {

            skeletonAnimation.CustomMaterialOverride[origMats[i]] = whiteMats[i];

        }
        CancelInvoke("ReturnFromWhiteFlash");
        Invoke("ReturnFromWhiteFlash", .05f);
    }

    public void ReturnFromWhiteFlash()
    {
        for (int i = 0; i < whiteMats.Length; i++)
        {
            skeletonAnimation.CustomMaterialOverride[origMats[i]] = origMats[i];

        }
    }
}

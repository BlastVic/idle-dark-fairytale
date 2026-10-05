using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSetter : MonoBehaviour
{
    public string animationToUse;
    private void OnEnable()
    {
        GetComponent<Animator>().CrossFade(animationToUse, 0);
    }

    public void OpenChestNow()
    {
        Animator anim = GetComponent<Animator>();
        anim.CrossFade("Chest", 0, 0, .5f);
        // anim.SetFloat("progress", .5f);
        //anim.StartPlayback();
    }

    public GameObject cherryParticle;
    public void PlayCherries()
    {
        cherryParticle.SetActive(true);
    }
}
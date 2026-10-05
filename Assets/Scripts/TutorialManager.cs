using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Services.Statistics;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private static TutorialManager _instance;
    public static TutorialManager single
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<TutorialManager>();
            return _instance;
        }
    }

    public List<GameObject> tutorials = new List<GameObject>();


    //    Tutorial Order:

    //1)pointing to the world map btn
    //    (Let's start your journey by checking out our options)
    //2)pointing on the very first node
    //    (Oh wow, would you look at this. Let's jump right in!)
    //3)pointong on stat pts back in camp, saying get HP
    //    (Damn, we must get stronger. Let's spend our new stat point on more health)
    //4)pointing on Inventory btn
    //    (What's that? Looks like there's new loot waiting us)
    //5)pointing on equip btn on the first Club when in Inventory
    //    (See that "Equip" button? I dare you to press it :p)
    //6)pointing on fuse btn on the other club
    //    (The rest of the crap...we don't need it, so let's fuse it)

    //7)pointing on last battle btn when in camp
    //    (Here, I'm gonna show you a shortcut)
    //8)pointing on treasure chest back in camp
    //    (Woah wait a second, what's with that chest :o)
    //9)when fusion is full, pointing to the fuse btn
    //    (Remember when we fused our crap gear? Now let's reap the benefits!)

    bool cool = true;

    void Cooldown()
    {
        cool = true;
    }
    public void PlayTutorial(int key)
    {
        if (GameManager.single.HasSeenThis("Tutorial-" + key))
        {
            //Debug.Log("Already seen tutorial:" + key);
            return;
        }

        //enforce chronological order to tutorials
        if (key > 0 && !GameManager.single.HasSeenThis("Tutorial-" + (key - 1)))
        {
            //Debug.Log("Haven't seen prerequisiste Tutorial");
            return;
        }

        cool = false;
        Invoke("Cooldown", .5f);

        StatisticsTracker.Instance.LogFlurryEvent("Tutorial-" + key);
        GameManager.single.thingsSeenPermanent.Add("Tutorial-" + key);


        tutorialCharacter.SetActive(true);
        tutorials[key].SetActive(true);
        //upon turning on bubble, an object called anchor moves the girl to the right position
        overlay.SetActive(true);
    }

    public GameObject tutorialCharacter, overlay;

    public void ClosePopUp()
    {
        if (!cool) return;
        StartCoroutine(ClosePopUpRoutine());
    }

    IEnumerator ClosePopUpRoutine()
    {
        overlay.SetActive(false);
        tutorialCharacter.SetActive(false); //first turn this off, then shrink the bubble
        foreach (GameObject obj in tutorials)
        {
            if (obj.activeSelf) obj.GetComponent<Animator>().CrossFade("TutorialOut", 0);
        }
        yield return new WaitForSeconds(.6f);
        foreach (GameObject obj in tutorials)
        {
            obj.SetActive(false);
        }



    }
}

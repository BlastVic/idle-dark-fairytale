using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyLifebar : MonoBehaviour
{
    public bool isMiniBoss = false;
    public bool isBoss = false;
    public Text miniBossName;
    public Text hpText;
    public MeterMover hpMeter;
    Actor_Base ab;
    private void OnEnable()
    {
        ab = GetComponentInParent<Actor_Base>();
        if (isMiniBoss || isBoss)
        {
            miniBossName.text=ab.GetComponent<Enemy>().bossName;
          if (isBoss)  ab.gameObject.transform.position = new Vector3(0, ab.transform.position.y, ab.transform.position.z);
        }
        GetComponent<Canvas>().worldCamera = Camera.main;
        EventManager.OnHideLifebars += HandleHideLifebars;

    }

    void OnDisable()
    {
        EventManager.OnHideLifebars -= HandleHideLifebars;


    }

    void HandleHideLifebars()
    {
        gameObject.SetActive(false);
    }


    public void RefreshHp()
    {
        hpMeter.SetValue(null, ab.currentStat.hpNow / ab.currentStat.hpMax);
        hpText.text = (int)ab.currentStat.hpNow + "/" + (int)ab.currentStat.hpMax;
    }
}

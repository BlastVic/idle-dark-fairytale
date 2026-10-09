using UnityEngine;
using UnityEngine.UI;

/// <summary>Data/interaction only. All visuals are authored children of the saved UGUI prefab.</summary>
public sealed class CursedBattleHud : MonoBehaviour
{
    public Text waveText, levelText, hpText, xpText;
    public Image hpFill, xpFill;
    public Button backButton;
    public GameObject progressRow;
    public Image[] waveMarkers;
    public Color normalHp = Color.white;
    public Color shieldHp = new Color(.75f, .48f, 1f);
    public Color completedWave = new Color(.48f, .12f, .12f);
    public Color currentWave = new Color(.65f, .44f, .18f);
    public Color pendingWave = new Color(.73f, .67f, .55f);

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        backButton.interactable = true;
        backButton.onClick.AddListener(LeaveBattle);
    }

    void OnDisable()
    {
        if (backButton) backButton.onClick.RemoveListener(LeaveBattle);
    }

    void LeaveBattle()
    {
        if (!Router.single) return;
        backButton.interactable = false;
        Router.single.GoToCampFromBatle();
    }

    public void SetHealth(float current, float maximum, bool shield)
    {
        hpText.text = Utilities.ConvertNumber(current) + " / " + Utilities.ConvertNumber(maximum);
        hpFill.fillAmount = maximum > 0 ? Mathf.Clamp01(current / maximum) : 0;
        hpFill.color = shield ? shieldHp : normalHp;
    }

    public void SetExperience(int level, float current, float maximum)
    {
        float fraction = maximum > 0 ? Mathf.Clamp01(current / maximum) : 0;
        levelText.text = "Lv. " + level;
        xpText.text = Mathf.FloorToInt(fraction * 100) + "%";
        xpFill.fillAmount = fraction;
    }

    public void SetWave(int current, int total)
    {
        total = Mathf.Max(1, total);
        current = Mathf.Clamp(current, 1, total);
        waveText.text = "第 " + current + " / " + total + " 波";
        // For unusually long levels the exact counter remains, without misleading truncated dots.
        progressRow.SetActive(total <= waveMarkers.Length);
        for (int i = 0; i < waveMarkers.Length; i++)
        {
            var marker = waveMarkers[i];
            marker.transform.parent.parent.gameObject.SetActive(i < total);
            marker.color = i < current - 1 ? completedWave : i == current - 1 ? currentWave : pendingWave;
        }
    }
}

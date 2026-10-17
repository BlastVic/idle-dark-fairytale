using UnityEngine;

/// <summary>Selects authored shelter nodes using the existing persisted victory record.</summary>
public sealed class ShelterChapterBackdrop : MonoBehaviour
{
    public ShelterResponsiveLayout layout;
    public RectTransform forest, cabin;
    public Vector2 forestSize=new Vector2(1080,1920), cabinSize=new Vector2(1080,1920);
    public float forestFocalBottom=1160, cabinFocalBottom=1091;
    public string cabinUnlockLevel="Mount Zion";
    public bool IsCabin {get;private set;}
    bool initialized;
    void OnEnable(){Refresh();}
    void LateUpdate(){Refresh();}
    public void Refresh()
    {
        if(!layout || !forest || !cabin)return;
        var waves=WaveManager.single;
        var level=waves ? waves.GetWavesetByKey(cabinUnlockLevel) : null;
        bool unlocked=level!=null && level.waves.Length>0 && waves.GetWaveseReachedByKey(cabinUnlockLevel)>=level.waves.Length;
        if(initialized && unlocked==IsCabin)return;
        initialized=true;IsCabin=unlocked;
        forest.gameObject.SetActive(!unlocked);cabin.gameObject.SetActive(unlocked);
        layout.SelectScene(unlocked?cabin:forest,unlocked?cabinSize:forestSize,unlocked?cabinFocalBottom:forestFocalBottom);
    }
}

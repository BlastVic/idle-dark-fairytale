using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VictoryEndingButtonAuthoring
{
    const string Out="output/victory-ending/";
    static VictoryEndingButtonAuthoring(){EditorApplication.update+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"install.request"))return;File.Delete(Out+"install.request");const string path="Assets/DarkFairytale/UI/BattleResult/StorybookVictory.prefab";var g=PrefabUtility.LoadPrefabContents(path);try{Apply(g.GetComponent<StorybookBattleResultUI>());PrefabUtility.SaveAsPrefabAsset(g,path);File.WriteAllText(Out+"install.txt","PASS");}finally{PrefabUtility.UnloadPrefabContents(g);}};}
    public static void Apply(StorybookBattleResultUI ui){
        if(!ui.victory)return;
        var actions=ui.standardActions.transform.parent.Find("ChapterEndingActions") as RectTransform;
        if(!actions){actions=new GameObject("ChapterEndingActions",typeof(RectTransform)).GetComponent<RectTransform>();var source=(RectTransform)ui.standardActions.transform;actions.SetParent(source.parent,false);actions.anchorMin=source.anchorMin;actions.anchorMax=source.anchorMax;actions.pivot=source.pivot;actions.sizeDelta=source.sizeDelta;actions.anchoredPosition=source.anchoredPosition;actions.localScale=source.localScale;}
        var go=ui.watchEnding?ui.watchEnding.gameObject:Object.Instantiate(ui.home.gameObject,actions);go.transform.SetParent(actions,false);go.name="WatchChapterEnding";
        ui.watchEnding=go.GetComponent<Button>();ui.watchEnding.onClick=new Button.ButtonClickedEvent();
        ((RectTransform)go.transform).anchoredPosition=(((RectTransform)ui.home.transform).anchoredPosition+((RectTransform)ui.replay.transform).anchoredPosition)*.5f;
        go.GetComponentInChildren<Text>(true).text="观看章尾";go.SetActive(false);
    }
}

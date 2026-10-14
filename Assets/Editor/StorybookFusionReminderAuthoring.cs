using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookFusionReminderAuthoring
{
    public const string Path="Assets/DarkFairytale/UI/Notifications/StorybookFusionReminder.prefab";
    const string Equipment="Assets/DarkFairytale/UI/Equipment/StorybookEquipment.prefab";
    static StorybookFusionReminderAuthoring(){EditorApplication.update+=()=>{
        const string request="output/fusion-reminder/build.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
        File.Delete(request);try{Build();File.WriteAllText("output/fusion-reminder/build.txt","PASS");}catch(System.Exception e){File.WriteAllText("output/fusion-reminder/build.txt",e.ToString());}
    };}
    public static void Build()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Path))throw new System.InvalidOperationException("Already authored; edit the prefab directly.");
        var equipment=PrefabUtility.LoadPrefabContents(Equipment);
        var ui=equipment.GetComponent<StorybookEquipmentUI>();var layout=equipment.GetComponent<StorybookEquipmentLayout>();
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StorybookExperienceReminderAuthoring.Path));
        root.name="StorybookFusionReminder";
        var old=root.GetComponent<StorybookExperienceReminder>();
        var reminder=root.AddComponent<StorybookFusionReminder>();
        reminder.card=old.card;reminder.group=old.group;reminder.open=old.open;reminder.close=old.close;
        Object.DestroyImmediate(old);
        reminder.heading=reminder.card.Find("Heading").GetComponent<Text>();reminder.heading.text="融合经验 +10";
        reminder.progress=reminder.card.Find("Benefit").GetComponent<Text>();reminder.progress.text="13%";
        reminder.progress.rectTransform.anchoredPosition=new Vector2(110,-22);reminder.progress.rectTransform.sizeDelta=new Vector2(90,44);reminder.progress.alignment=TextAnchor.MiddleRight;
        reminder.open.GetComponentInChildren<Text>().text="前往融合";reminder.open.GetComponentInChildren<Text>().fontSize=27;
        var track=new GameObject("ProgressTrack",typeof(RectTransform),typeof(Image));track.transform.SetParent(reminder.card,false);
        var r=(RectTransform)track.transform;r.anchoredPosition=new Vector2(-143,-22);r.sizeDelta=new Vector2(380,28);
        var trackImage=track.GetComponent<Image>();trackImage.sprite=ui.fusionFill.sprite;trackImage.color=new Color(.17f,.14f,.12f);trackImage.raycastTarget=false;
        var fill=Object.Instantiate(ui.fusionFill,track.transform);fill.name="ProgressFill";fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=Vector2.one*3;fill.rectTransform.offsetMax=-Vector2.one*3;fill.raycastTarget=false;reminder.fill=fill;
        var rootRect=(RectTransform)root.transform;rootRect.localScale=Vector3.one;rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.pivot=Vector2.one*.5f;rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;root.GetComponent<Canvas>().overrideSorting=true;
        root.SetActive(true);reminder.card.gameObject.SetActive(false);reminder.group.alpha=0;reminder.group.blocksRaycasts=reminder.group.interactable=false;
        PrefabUtility.SaveAsPrefabAsset(root,Path);Object.DestroyImmediate(root);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path),equipment.transform);
        ui.fusionReminder=instance.GetComponent<StorybookFusionReminder>();
        ui.fusionFill=null;ui.fusionText=null;
        var oldPanel=layout.fusion;layout.fusion=null;layout.fusionContent=null;
        if(oldPanel)Object.DestroyImmediate(oldPanel.gameObject);
        PrefabUtility.SaveAsPrefabAsset(equipment,Equipment);PrefabUtility.UnloadPrefabContents(equipment);AssetDatabase.SaveAssets();
    }
}

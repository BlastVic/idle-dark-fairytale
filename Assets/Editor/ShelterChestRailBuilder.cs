using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit additive prefab patch. Re-import and Play never author or overwrite nodes.</summary>
[InitializeOnLoad]
public static class ShelterChestRailBuilder
{
    public const string Out="output/chest-rail/";
    static ShelterChestRailBuilder(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"build.request"))return;
        File.Delete(Out+"build.request");
        try{Patch();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}
    }
    static RectTransform Node(Transform parent,string name)
    {var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
    static void Stretch(RectTransform rect,float right=0)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=Vector2.one*.5f;rect.offsetMin=Vector2.zero;rect.offsetMax=new Vector2(-right,0);}
    [MenuItem("Tools/Dark Fairytale/UI/Add Chest Rail Collapse (Explicit)")]
    public static void Patch()
    {
        Directory.CreateDirectory(Out);
        var root=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);
        try
        {
            // Preserve manual edits if the feature is already installed.
            if(root.GetComponent<ShelterChestRail>())return;
            var home=root.GetComponent<ShelterHomeUI>();var layout=root.GetComponent<ShelterResponsiveLayout>();
            var rail=layout.chests;int sibling=rail.GetSiblingIndex();
            var slot=Node(rail.parent,"ChestRailSlot");slot.SetSiblingIndex(sibling);
            slot.anchorMin=rail.anchorMin;slot.anchorMax=rail.anchorMax;slot.pivot=rail.pivot;slot.sizeDelta=rail.sizeDelta;slot.anchoredPosition=rail.anchoredPosition;
            rail.SetParent(slot,false);Stretch(rail);layout.chests=slot;
            // Reserve a narrow right column; all existing chest bindings and status plates stay intact.
            var slots=Node(rail,"ChestSlots");Stretch(slots,96);
            var oldChildren=new System.Collections.Generic.List<Transform>();
            foreach(Transform child in rail)if(child!=slots&&child.name!="ReferenceFrame")oldChildren.Add(child);
            foreach(var child in oldChildren)child.SetParent(slots,false);
            var drawer=root.AddComponent<ShelterChestRail>();drawer.home=home;drawer.layout=layout;drawer.railSlot=slot;drawer.rail=rail;
            drawer.railGroup=rail.GetComponent<CanvasGroup>();
            if(!drawer.railGroup)drawer.railGroup=rail.gameObject.AddComponent<CanvasGroup>();
            var collapse=Node(rail,"CollapseRail");collapse.anchorMin=collapse.anchorMax=new Vector2(1,.5f);collapse.pivot=Vector2.one*.5f;collapse.anchoredPosition=new Vector2(-50,0);collapse.sizeDelta=new Vector2(84,134);
            var hit=collapse.gameObject.AddComponent<Image>();hit.color=new Color(1,1,1,.001f);
            var button=collapse.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.navigation=new Navigation{mode=Navigation.Mode.None};
            collapse.gameObject.AddComponent<ShelterButtonFeedback>();collapse.gameObject.AddComponent<StorybookEquipmentButtonState>().grayscaleShader=Shader.Find("UI/Storybook Disabled");
            var circle=Node(collapse,"PaperMedallion");ShelterRefinedBuilder.Box(circle,14,16,56,60);var circleImage=circle.gameObject.AddComponent<Image>();circleImage.sprite=home.functionButtons[0].GetComponent<Image>().sprite;circleImage.material=home.functionButtons[0].GetComponent<Image>().material;circleImage.preserveAspect=true;circleImage.raycastTarget=false;
            var fill=UnityEngine.Object.Instantiate(home.functionButtons[0].transform.Find("ReferenceCirclePaper").gameObject,circle).GetComponent<RectTransform>();fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=new Vector2(4,4);fill.offsetMax=new Vector2(-4,-4);
            for(int i=0;i<2;i++){var stroke=Node(circle,"ChevronStroke_"+i);stroke.anchorMin=stroke.anchorMax=stroke.pivot=Vector2.one*.5f;stroke.anchoredPosition=new Vector2(1,i==0?5:-5);stroke.sizeDelta=new Vector2(16,3.4f);stroke.localEulerAngles=new Vector3(0,0,i==0?-40:40);var ink=stroke.gameObject.AddComponent<Image>();ink.color=new Color(.12f,.09f,.06f);ink.raycastTarget=false;}
            var caption=Node(collapse,"Caption");ShelterRefinedBuilder.Box(caption,0,87,84,32);var label=caption.gameObject.AddComponent<Text>();label.font=home.functionButtons[0].GetComponentInChildren<Text>().font;label.fontSize=22;label.text="收起";label.color=new Color(.12f,.09f,.06f);label.alignment=TextAnchor.MiddleCenter;label.verticalOverflow=VerticalWrapMode.Overflow;label.raycastTarget=false;
            drawer.collapseButton=button;
            var shortcut=UnityEngine.Object.Instantiate(home.functionButtons[0].gameObject,layout.core).GetComponent<RectTransform>();shortcut.name="ChestShortcut";
            drawer.shortcut=shortcut;drawer.expandButton=shortcut.GetComponent<Button>();drawer.expandButton.onClick=new Button.ButtonClickedEvent();
            drawer.shortcutGroup=shortcut.GetComponent<CanvasGroup>();if(!drawer.shortcutGroup)drawer.shortcutGroup=shortcut.gameObject.AddComponent<CanvasGroup>();drawer.shortcutGroup.interactable=false;drawer.shortcutGroup.blocksRaycasts=false;
            var icon=shortcut.Find("Icon").GetComponent<Image>();icon.sprite=home.chestSprites[0];icon.material=home.chestImages[0].material;icon.color=Color.white;icon.preserveAspect=true;ShelterRefinedBuilder.Box(icon.rectTransform,26,26,82,48);
            shortcut.Find("Caption").GetComponent<Text>().text="宝箱";
            for(int i=shortcut.childCount-1;i>=0;i--){var child=shortcut.GetChild(i);if(child.name!="Icon"&&child.name!="Caption"&&child.name!="ReferenceCirclePaper")UnityEngine.Object.DestroyImmediate(child.gameObject);}
            layout.Apply();var first=layout.functions[0];shortcut.anchorMin=shortcut.anchorMax=shortcut.pivot=new Vector2(0,1);shortcut.sizeDelta=first.sizeDelta;shortcut.anchoredPosition=first.anchoredPosition+new Vector2(0,layout.functionGap);
            shortcut.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,ShelterHomeBuilder.Path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}

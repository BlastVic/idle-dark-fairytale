using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EquipmentCompactComparisonPatch
{
    const string Out="output/equipment-compact-comparison/";
    static EquipmentCompactComparisonPatch(){EditorApplication.update+=Tick;}
    static void Box(Transform t,float x,float y,float w,float h,float lo,float hi){
        var r=(RectTransform)t;StorybookEquipmentLayout.Box(r,x,y,w,h);
        r.anchorMin=new Vector2(lo,1);r.anchorMax=new Vector2(hi,1);
        r.offsetMin=new Vector2(x-lo*925,-y-h);r.offsetMax=new Vector2(x+w-hi*925,-y);
    }
    static void Tick(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"patch.request"))return;
        File.Delete(Out+"patch.request");
        const string path="Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var ui=root.GetComponent<StorybookEquipmentUI>();var p=ui.layout.detailContent;
            if(p.Find("ReplaceDirection")){
                var arrows=p.Cast<Transform>().Where(t=>t.name=="ReplaceDirection").ToArray();
                foreach(var extra in arrows.Skip(1))Object.DestroyImmediate(extra.gameObject);
                if(!arrows[0].GetComponent<CanvasRenderer>())arrows[0].gameObject.AddComponent<CanvasRenderer>();
                arrows[0].gameObject.layer=p.gameObject.layer;
                var ar=(RectTransform)arrows[0];ar.anchorMin=ar.anchorMax=new Vector2(1f/3f,1);ar.pivot=Vector2.one*.5f;ar.sizeDelta=new Vector2(19,22);ar.anchoredPosition=new Vector2(243-925f/3f,-77);
                var allRules=p.Cast<Transform>().Where(t=>t.name=="InkRule").ToArray();
                for(int i=0;i<3;i++){
                    var rule=Object.Instantiate(allRules[0].gameObject,p);rule.name="InkRule";
                    Box(rule.transform,243+i*230,18,1,i==0?44:293,(i+1)/3f,(i+1)/3f);
                }
                var bottom=Object.Instantiate(allRules[0].gameObject,p);bottom.name="InkRule";Box(bottom.transform,243,92,1,219,1f/3f,1f/3f);
                foreach(var rule in allRules)Object.DestroyImmediate(rule.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS renderer repaired");return;
            }
            ui.layout.detailHeight=325;
            var icons=new[]{ui.equippedIcon,ui.selectedIcon};var names=new[]{ui.equippedName,ui.selectedName};var grades=new[]{ui.equippedGrade,ui.grade};
            for(int i=0;i<2;i++){
                float x=24+i*230;
                Box(icons[i].transform,x,59,38,38,i/3f,i/3f);icons[i].preserveAspect=true;
                Box(names[i].transform,x+46,58,164,40,i/3f,(i+1)/3f);
                names[i].alignment=TextAnchor.MiddleLeft;
                if(!names[i].GetComponent<EquipmentSingleLineName>())names[i].gameObject.AddComponent<EquipmentSingleLineName>();
                Box(grades[i].transform,x+46,98,164,24,i/3f,(i+1)/3f);grades[i].alignment=TextAnchor.MiddleLeft;
            }
            var view=ui.comparisonScroll.viewport;var pos=view.anchoredPosition;pos.y=-126;view.anchoredPosition=pos;
            var rules=p.Cast<Transform>().Where(t=>t.name=="InkRule").OrderBy(t=>((RectTransform)t).anchoredPosition.x).ToArray();
            foreach(var rule in rules){var r=(RectTransform)rule;r.sizeDelta=new Vector2(r.sizeDelta.x,279);}
            var line=rules[0];
            // Keep the divider editable and split it around a persistent directional icon.
            var lineRect=(RectTransform)line;lineRect.sizeDelta=new Vector2(1,44);
            var tail=Object.Instantiate(line.gameObject,p);tail.name="InkRule";
            var tr=(RectTransform)tail.transform;tr.anchoredPosition=new Vector2(lineRect.anchoredPosition.x,-91);tr.sizeDelta=new Vector2(1,206);
            var arrow=new GameObject("ReplaceDirection",typeof(RectTransform),typeof(CanvasRenderer),typeof(EquipmentDeltaArrow));arrow.transform.SetParent(p,false);
            var graphic=arrow.GetComponent<EquipmentDeltaArrow>();graphic.color=new Color(.43f,.18f,.10f);graphic.raycastTarget=false;
            Box(arrow.transform,232,66,22,19,1/3f,1/3f);((RectTransform)arrow.transform).pivot=new Vector2(.5f,.5f);arrow.transform.localRotation=Quaternion.Euler(0,0,-90);
            // Two-row comparisons are 214 high, so keep the actions within that minimum.
            Box(ui.equip.transform,738,52,162,53,1,1);Box(ui.decompose.transform,738,115,162,53,1,1);Box(ui.lockButton.transform,770,173,98,27,1,1);
            var swipe=(RectTransform)p.Find("DetailsSwipeSurface");swipe.sizeDelta=new Vector2(swipe.sizeDelta.x,284);
            PrefabUtility.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}

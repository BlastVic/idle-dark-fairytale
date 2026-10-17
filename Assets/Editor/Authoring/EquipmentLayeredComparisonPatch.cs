using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EquipmentLayeredComparisonPatch
{
    const string Out="output/equipment-layered-comparison/";
    static EquipmentLayeredComparisonPatch(){EditorApplication.update+=Tick;}
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
            if(ui.layeredComparison){
                foreach(var icon in new[]{ui.equippedIcon,ui.selectedIcon}){
                    icon.rectTransform.pivot=Vector2.one*.5f;icon.rectTransform.offsetMin=new Vector2(12,3);icon.rectTransform.offsetMax=new Vector2(-12,-3);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS centered artwork");return;
            }
            ui.layeredComparison=true;ui.minimumComparisonHeight=214;ui.layout.detailHeight=303;
            var viewport=ui.comparisonScroll.viewport;viewport.anchoredPosition=new Vector2(viewport.anchoredPosition.x,-104);
            viewport.sizeDelta=new Vector2(viewport.sizeDelta.x,185);
            var names=new[]{ui.equippedName,ui.selectedName};var icons=new[]{ui.equippedIcon,ui.selectedIcon};
            ui.grade.gameObject.SetActive(false);ui.equippedGrade.gameObject.SetActive(false);
            for(int i=0;i<2;i++){
                Box(names[i].transform,24+i*230,53,205,40,i/3f,(i+1)/3f);
                names[i].alignment=TextAnchor.MiddleLeft;
                var fit=names[i].GetComponent<EquipmentSingleLineName>();fit.maximumSize=20;fit.minimumSize=16;
                var layer=new GameObject(i==0?"CurrentEquipmentBackdrop":"TargetEquipmentBackdrop",typeof(RectTransform));layer.layer=viewport.gameObject.layer;
                var rect=(RectTransform)layer.transform;rect.SetParent(viewport,false);rect.SetAsFirstSibling();
                rect.anchorMin=new Vector2(i/3f,0);rect.anchorMax=new Vector2((i+1)/3f,1);rect.offsetMin=new Vector2(i*3+2,0);rect.offsetMax=new Vector2(-16+i*3,0);
                icons[i].transform.SetParent(rect,false);var ir=icons[i].rectTransform;ir.pivot=Vector2.one*.5f;ir.anchorMin=Vector2.zero;ir.anchorMax=Vector2.one;ir.offsetMin=new Vector2(12,3);ir.offsetMax=new Vector2(-12,-3);
                icons[i].preserveAspect=true;icons[i].color=new Color(1,1,1,.65f);icons[i].raycastTarget=false;
                var veil=new GameObject("TranslucentParchment",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));veil.layer=rect.gameObject.layer;
                var vr=(RectTransform)veil.transform;vr.SetParent(rect,false);vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;
                var image=veil.GetComponent<Image>();image.color=new Color(.86f,.79f,.65f,.45f);image.raycastTarget=false;
            }
            foreach(var t in p.Cast<Transform>().Where(t=>t.name=="InkRule"||t.name=="ReplaceDirection").ToArray())Object.DestroyImmediate(t.gameObject);
            for(int i=0;i<3;i++){
                var g=new GameObject(i==0?"ReplacementDivider":"ComparisonRule",typeof(RectTransform),typeof(CanvasRenderer));g.layer=p.gameObject.layer;
                var r=(RectTransform)g.transform;r.SetParent(p,false);r.anchorMin=new Vector2((i+1)/3f,0);r.anchorMax=new Vector2((i+1)/3f,1);r.pivot=new Vector2(0,.5f);
                float x=243+i*230-(i+1)*925f/3f;
                r.offsetMin=new Vector2(x-(i==0?2:0),14);r.offsetMax=new Vector2(x+(i==0?14:1),-18);
                if(i==0){var line=g.AddComponent<EquipmentReplacementDivider>();line.color=new Color(.37f,.25f,.13f,.85f);line.raycastTarget=false;line.centerOffset=45;}
                else{var line=g.AddComponent<Image>();line.color=new Color(.25f,.20f,.15f,.22f);line.raycastTarget=false;}
            }
            var swipe=(RectTransform)p.Find("DetailsSwipeSurface");swipe.anchorMin=new Vector2(0,0);swipe.anchorMax=Vector2.one;swipe.offsetMin=new Vector2(17,14);swipe.offsetMax=new Vector2(-220,-17);
            // Opaque labels remain above the watermark and veil. Main attributes are bold.
            foreach(var row in ui.comparisonRows){
                bool primary=row==ui.comparisonRows[1];
                foreach(var label in row.root.GetComponentsInChildren<Text>(true)){
                    if(label==row.delta)continue;
                    label.color=new Color(.08f,.06f,.04f,1);label.fontStyle=primary?FontStyle.Bold:FontStyle.Normal;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);File.WriteAllText(Out+"patch.txt","PASS");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}

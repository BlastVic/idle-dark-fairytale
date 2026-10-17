using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class AttributeAvailabilityPatch
{
    const string Out="output/attribute-availability/";
    static AttributeAvailabilityPatch(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Out+"patch.request"))return;
        File.Delete(Out+"patch.request");var root=PrefabUtility.LoadPrefabContents(StorybookAttributeAuthoring.Path);
        try{
            var panel=root.GetComponent<StorybookAttributePanel>();
            if(panel.unavailableBackgrounds!=null&&panel.unavailableBackgrounds.Length==6){foreach(var caption in panel.unavailableCaptions){caption.fontSize=16;caption.rectTransform.offsetMax=new Vector2(-8,21);}
                PrefabUtility.SaveAsPrefabAsset(root,StorybookAttributeAuthoring.Path);File.WriteAllText(Out+"patch.txt","PASS larger reason labels");return;}
            panel.unavailableBackgrounds=new Image[6];panel.unavailableCaptions=new Text[6];
            for(int i=0;i<6;i++){
                var cell=panel.add[i].transform.parent;
                var g=new GameObject("UnavailableBackground",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.layer=cell.gameObject.layer;g.transform.SetParent(cell,false);g.transform.SetAsFirstSibling();
                var r=(RectTransform)g.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(2,2);r.offsetMax=new Vector2(-2,-2);
                var image=g.GetComponent<Image>();image.color=new Color(.20f,.20f,.20f,.34f);image.raycastTarget=false;panel.unavailableBackgrounds[i]=image;
                var label=Object.Instantiate(panel.values[i].gameObject,cell);label.name="UnavailableReason";
                var text=label.GetComponent<Text>();text.text="不可连续加点";text.fontSize=16;text.resizeTextForBestFit=false;text.fontStyle=FontStyle.Normal;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.20f,.16f,.12f,1);text.raycastTarget=false;
                var lr=text.rectTransform;lr.anchorMin=new Vector2(0,0);lr.anchorMax=new Vector2(1,0);lr.pivot=new Vector2(.5f,0);lr.offsetMin=new Vector2(8,2);lr.offsetMax=new Vector2(-8,21);panel.unavailableCaptions[i]=text;
                foreach(Transform child in cell){if(child==g.transform||child==label.transform||child.name=="CellFrame")continue;var cr=child as RectTransform;if(cr)cr.anchoredPosition+=Vector2.up*5;}
                g.SetActive(false);label.SetActive(false);
            }
            PrefabUtility.SaveAsPrefabAsset(root,StorybookAttributeAuthoring.Path);File.WriteAllText(Out+"patch.txt","PASS");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}

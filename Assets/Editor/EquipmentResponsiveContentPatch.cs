using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentResponsiveContentPatch
{
    static EquipmentResponsiveContentPatch(){EditorApplication.update+=Tick;}
    // Preserve the authored geometry at the reference width while assigning growth to anchors.
    static void Anchor(Transform t,float lo,float hi,float width){
        if(!t)return;var r=(RectTransform)t;
        float left=r.anchorMin.x*width+r.offsetMin.x,right=r.anchorMax.x*width+r.offsetMax.x;
        var amin=r.anchorMin;var amax=r.anchorMax;amin.x=lo;amax.x=hi;r.anchorMin=amin;r.anchorMax=amax;
        var min=r.offsetMin;var max=r.offsetMax;min.x=left-lo*width;max.x=right-hi*width;r.offsetMin=min;r.offsetMax=max;
    }
    static void Tick(){
        const string dir="output/equipment-responsive/";
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(dir+"patch.request"))return;
        File.Delete(dir+"patch.request");
        const string path="Assets/DarkFairytale/UI/Equipment/StorybookEquipment.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var ui=root.GetComponent<StorybookEquipmentUI>();var l=ui.layout;
            // Migrate only once; future Inspector adjustments must not be overwritten.
            if(l.responsiveContents){File.WriteAllText(dir+"patch.txt","Already applied");return;}
            foreach(var c in new[]{l.categoryContent,l.toolbar,l.detailContent,l.attributeContent})c.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,925);
            for(int i=0;i<5;i++)Anchor(ui.categoryButtons[i].transform,i/4f,i/4f,925);
            foreach(var b in new[]{ui.sortButton,ui.filterButton,ui.batchButton,ui.compactModeButton,ui.detailedModeButton})Anchor(b.transform,1,1,925);
            foreach(Transform t in l.toolbar)if(t.name=="InkRule")Anchor(t,0,1,925);
            var p=l.detailContent;
            string[][] columns={new[]{"CurrentHeading","EquippedName","EquippedGrade"},new[]{"TargetHeading","SelectedName","SelectedGrade"},new[]{"DeltaHeading","ChangeCaption"}};
            for(int i=0;i<3;i++)foreach(var name in columns[i])Anchor(p.Find(name),i/3f,(i+1)/3f,925);
            Anchor(ui.equippedIcon.transform,1/6f,1/6f,925);Anchor(ui.selectedIcon.transform,.5f,.5f,925);
            foreach(var name in new[]{"ActionHeading","Equip","Decompose","ToggleItemLock"})Anchor(p.Find(name),1,1,925);
            var rules=p.Cast<Transform>().Where(t=>t.name=="InkRule").OrderBy(t=>((RectTransform)t).anchoredPosition.x).ToArray();
            for(int i=0;i<rules.Length;i++)Anchor(rules[i],(i+1)/3f,(i+1)/3f,925);
            Anchor(p.Find("DetailsSwipeSurface"),0,1,925);Anchor(ui.comparisonScroll.viewport,0,1,925);
            ui.comparisonContent.anchorMin=new Vector2(0,1);ui.comparisonContent.anchorMax=new Vector2(1,1);ui.comparisonContent.sizeDelta=new Vector2(0,ui.comparisonContent.sizeDelta.y);
            foreach(var row in ui.comparisonRows){
                for(int c=0;c<3;c++){
                    Anchor(row.root.Find("StatIcon"+c),c/3f,c/3f,681);Anchor(row.root.Find("Label"+c),c/3f,c/3f,681);
                }
                Anchor(row.current.transform,0,1/3f,681);Anchor(row.selected.transform,1/3f,2/3f,681);
                Anchor(row.delta.transform,1,1,681);Anchor(row.deltaArrow.transform,1,1,681);
                var lines=row.root.Cast<Transform>().Where(t=>t.name=="InkRule").OrderBy(t=>((RectTransform)t).anchoredPosition.x).ToArray();
                for(int c=0;c<lines.Length;c++)Anchor(lines[c],c/3f,(c+1)/3f,681);
                Anchor(row.root,0,1,681);
            }
            Anchor(ui.points.transform,1,1,925);Anchor(ui.foldButton.transform,0,1,925);Anchor(ui.foldArrow,1,1,887);
            var body=(RectTransform)ui.attributeBody.transform;Anchor(body,0,1,925);
            var cells=body.Cast<Transform>().Where(t=>t.name.StartsWith("Stat_")).ToArray();
            for(int i=0;i<cells.Length;i++){
                var cell=cells[i];int c=i%3;Anchor(cell,c/3f,(c+1)/3f,877);
                Anchor(cell.Find("CellFrame"),0,1,279);Anchor(cell.Find("Value"),0,1,279);Anchor(cell.Find("AddStat"),1,1,279);
            }
            foreach(Transform t in body)if(t.name=="InkRule")Anchor(t,0,1,877);
            Anchor(body.Find("TotalsCaption"),.5f,.5f,877);
            l.responsiveContents=true;l.Apply();
            PrefabUtility.SaveAsPrefabAsset(root,path);File.WriteAllText(dir+"patch.txt","PASS: equipment contents anchored across available width");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}

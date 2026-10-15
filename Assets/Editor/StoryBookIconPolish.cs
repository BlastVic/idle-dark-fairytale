using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class StoryBookIconPolish
{
    const string Dir="output/story-book-icon/",Key="STORY_BOOK_ICON_QA";
    static bool running;
    static StoryBookIconPolish(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"apply.request")){
            File.Delete(Dir+"apply.request");
            var ti=(TextureImporter)AssetImporter.GetAtPath("Assets/DarkFairytale/UI/Map/Art/StoryBookCover.png");
            ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Bilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
            var root=PrefabUtility.LoadPrefabContents(StorybookMapBuilder.Path);
            try{foreach(var node in root.GetComponent<StorybookMapUI>().nodes.Where(n=>n.IsStory)){var mark=node.seal.transform.Find("StoryMark");if(mark)Object.DestroyImmediate(mark.gameObject);}PrefabUtility.SaveAsPrefabAsset(root,StorybookMapBuilder.Path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            File.WriteAllText(Dir+"validation.txt","");SessionState.SetBool(Key,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying&&SessionState.GetBool(Key,false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Render());}
    }
    static IEnumerator Render(){
        var intro=Object.FindAnyObjectByType<CursedBookIntro>();while(intro&&!intro.Finished)yield return null;
        var gc=GameplayCanvas.single;TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();gc.dailyCherriesPop.SetActive(false);
        gc.homeUI.navigationButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(2);
        var ui=Object.FindAnyObjectByType<StorybookMapUI>();
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return null;
            for(int i=0;i<ui.nodes.Length;i++)if(ui.nodes[i].IsStory){
                ui.SelectNode(i);ui.LocateSelection();ui.Refresh();yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();
                var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+ui.nodes[i].name+"-"+size.x+"x"+size.y+".png",shot.EncodeToPNG());Object.Destroy(shot);
                File.AppendAllText(Dir+"validation.txt",(!ui.nodes[i].seal.transform.Find("StoryMark")?"PASS ":"FAIL ")+ui.nodes[i].name+" no cover text "+size+"\n");
            }
        }
        File.AppendAllText(Dir+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}

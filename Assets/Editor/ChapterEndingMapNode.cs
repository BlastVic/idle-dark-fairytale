using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ChapterEndingMapNode
{
    const string Dir="output/chapter-ending-map/", Key="ENDING_MAP_QA";
    static bool running;
    static ChapterEndingMapNode(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;}};}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Dir+"install.request")){
            File.Delete(Dir+"install.request");
            var root=PrefabUtility.LoadPrefabContents(StorybookMapBuilder.Path);
            try{
                var ui=root.GetComponent<StorybookMapUI>();
                var node=ui.nodes.FirstOrDefault(n=>n.name=="Story_Chapter01Ending");
                if(!node){node=Object.Instantiate(ui.nodes.First(n=>n.IsStory),ui.nodes[0].transform.parent);ui.nodes=ui.nodes.Concat(new[]{node}).ToArray();}
                node.name="Story_Chapter01Ending";node.story=AssetDatabase.LoadAssetAtPath<StorySequence>(Chapter01EndingBuilder.Root+"Chapter01Ending.asset");
                node.storyPlayerOverride=AssetDatabase.LoadAssetAtPath<GameObject>(Chapter01EndingBuilder.Root+"Chapter01Ending.prefab").GetComponent<CursedBookIntro>();
                node.storyUnlockLevelKey="Mount Zion";node.displayName="章末 · 猎人小屋";node.label.text=node.displayName;
                ((RectTransform)node.transform).anchoredPosition=new Vector2(475,-300);
                node.progress.text="剧情 · 可重播";
                string previewPath="Assets/DarkFairytale/UI/Map/ChapterEndingPreview.asset";
                var preview=AssetDatabase.LoadAssetAtPath<Sprite>(previewPath);
                if(!preview){var tex=node.story.pages[3].illustration;float h=tex.width*132f/218;preview=Sprite.Create(tex,new Rect(0,(tex.height-h)*.5f,tex.width,h),Vector2.one*.5f,100);AssetDatabase.CreateAsset(preview,previewPath);}
                node.preview=preview;PrefabUtility.SaveAsPrefabAsset(root,StorybookMapBuilder.Path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.WriteAllText(Dir+"validation.txt","");SessionState.SetBool(Key,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying&&SessionState.GetBool(Key,false)&&!running&&GameManager.single&&GameplayCanvas.single){running=true;GameManager.single.StartCoroutine(Run());}
    }
    static void Check(bool ok,string msg){File.AppendAllText(Dir+"validation.txt",(ok?"PASS ":"FAIL ")+msg+"\n");}
    static IEnumerator Run(){
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&!intro.Finished)yield return null;
        var gc=GameplayCanvas.single;var gm=GameManager.single;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();gc.dailyCherriesPop.SetActive(false);
        gc.homeUI.navigationButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(2);
        var ui=Object.FindFirstObjectByType<StorybookMapUI>();int index=System.Array.FindIndex(ui.nodes,n=>n.name=="Story_Chapter01Ending");
        Check(index>=0&&ui.nodes.Count(n=>!n.IsStory)==5,"saved ending node; five battles unchanged");
        if(!gm.HasSeenThis(CursedBookIntro.ChapterEndingSeenKey))gm.thingsSeenPermanent.Add(CursedBookIntro.ChapterEndingSeenKey);
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)}){
            StorybookEquipmentValidation.Resolution(size.x,size.y);yield return null;ui.SelectNode(index);ui.LocateSelection();ui.Refresh();yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();
            var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+size.x+"x"+size.y+".png",shot.EncodeToPNG());Object.Destroy(shot);
        }
        for(int repeat=0;repeat<2;repeat++){
            var pos=ui.scroll.content.anchoredPosition;ui.enterButton.onClick.Invoke();yield return null;
            Check(ui.ActiveStory&&ui.ActiveStory.chapterEnding,"dedicated ending player started");
            bool[] pages=new bool[5];float deadline=Time.realtimeSinceStartup+90;
            while(ui.ActiveStory&&Time.realtimeSinceStartup<deadline){var p=ui.ActiveStory.prologue;if(p.Reading&&p.PageReadTime>.4f&&p.PageIndex>=0&&p.PageIndex<5&&!pages[p.PageIndex]){pages[p.PageIndex]=true;if(p.narration.isPlaying)p.narration.time=p.narration.clip.length-.1f;}yield return null;}
            Check(pages.All(x=>x),"all five pages including live cabin narrated");
            Check(!ui.ActiveStory&&!ui.IsNavigating&&ui.controls.interactable&&!gc.homeUI.gameObject.activeSelf,"returns to interactive map without home overlay");
            Check(Vector2.Distance(pos,ui.scroll.content.anchoredPosition)<1,"original map scroll restored");
        }
        File.AppendAllText(Dir+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}

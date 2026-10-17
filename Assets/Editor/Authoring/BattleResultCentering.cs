using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit prefab correction and isolated rendering regression; never runs without a request.
[InitializeOnLoad]
public static class BattleResultCentering
{
    const string Dir = "output/result-centering/", Key = "RESULT_CENTERING_QA";
    static bool running;
    static BattleResultCentering()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false); SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false); running = false;
        };
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Dir + "apply.request"))
        {
            File.Delete(Dir + "apply.request");
            var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(StorybookBattleResultBuilder.Root + "/ResultPanelShared.png"));
            var pixels = texture.GetPixels32(); int left=texture.width, right=0, bottom=texture.height, top=0;
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) if(pixels[y*texture.width+x].a>32) {left=Mathf.Min(left,x);right=Mathf.Max(right,x+1);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y+1);}
            var center = new Vector2((left+right)*.5f/texture.width, (bottom+top)*.5f/texture.height);
            Object.DestroyImmediate(texture);
            foreach (var name in new[]{"StorybookVictory", "StorybookDefeat"})
            {
                string path=StorybookBattleResultBuilder.Root+"/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try {
                    var core=(RectTransform)root.transform.Find("Core_9x16");
                    var art=(RectTransform)core.Find("ApprovedArtwork_NoText");
                    Vector2 desired=Vector2.Scale(Vector2.one*.5f-center,art.sizeDelta);
                    Vector2 delta=desired-art.anchoredPosition;
                    foreach(RectTransform child in core) child.anchoredPosition+=delta;
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                } finally {PrefabUtility.UnloadPrefabContents(root);}
            }
            File.WriteAllText(Dir+"validation.txt", "Visible artwork center: "+center+"\n");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            SessionState.SetBool(Key,true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;
        }
        if(SessionState.GetBool(Key,false)&&EditorApplication.isPlaying&&!running&&GameManager.single&&GameplayCanvas.single)
        {running=true;GameManager.single.StartCoroutine(Render());}
    }
    static IEnumerator Render()
    {
        var intro=Object.FindFirstObjectByType<CursedBookIntro>();while(intro&&intro.gameObject.activeSelf)yield return null;
        TutorialManager.single.automatic=false;TutorialManager.single.view.Hide();
        var gc=GameplayCanvas.single;gc.dailyCherriesPop.SetActive(false);
        foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(768,1024),new Vector2Int(720,1440)})
        {
            StorybookEquipmentValidation.Resolution(size.x,size.y);
            foreach(var panel in new[]{gc.victoryPanel,gc.defeatPanel})
            {
                gc.victoryPanel.SetActive(false);gc.defeatPanel.SetActive(false);panel.SetActive(true);
                var ui=panel.GetComponent<StorybookBattleResultUI>();
                if(ui.victory){ui.chestImage.sprite=ui.normalChest;ui.chestImage.enabled=true;}
                yield return new WaitForSecondsRealtime(.6f);
                yield return new WaitForEndOfFrame();
                var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Dir+panel.name+"-"+size.x+"x"+size.y+".png",image.EncodeToPNG());Object.Destroy(image);
                File.AppendAllText(Dir+"validation.txt","Rendered "+panel.name+" "+size+"\n");
                panel.SetActive(false);
            }
        }
        File.AppendAllText(Dir+"validation.txt","DONE\n");EditorApplication.isPlaying=false;
    }
}

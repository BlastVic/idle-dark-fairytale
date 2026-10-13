using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using Assets.Scripts.Game.Enum;

/// <summary>Opt-in true startup runs, including exact last-page / live-home seam captures.</summary>
[InitializeOnLoad]
public static class CursedBookIntroValidation
{
    public const string Dir = "output/storybook-intro/page-entry/";
    static CursedBookIntroValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BOOK_ENTRY_QA", false))
                StartProbe();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("BOOK_ENTRY_LOCKED", false)) {
                EditorApplication.UnlockReloadAssemblies(); SessionState.SetBool("BOOK_ENTRY_LOCKED",false);
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("BOOK_ENTRY_QA", false)) {
                int next = SessionState.GetInt("BOOK_ENTRY_RATIO", 0) + 1;
                SessionState.SetInt("BOOK_ENTRY_RATIO", next);
                if (next < 3 && !SessionState.GetBool("BOOK_ENTRY_FAILED", false))
                    EditorApplication.delayCall += Begin;
                else {
                    SessionState.SetBool("BOOK_ENTRY_QA", false);
                    SessionState.SetBool("MONSTER_QA_DISABLE_SAVE", false);
                }
            }
        };
    }
    static void StartProbe()
    {
        if (SessionState.GetBool("BOOK_ENTRY_RUNNING",false) || !GameManager.single) return;
        SessionState.SetBool("BOOK_ENTRY_RUNNING",true);
        GameManager.single.StartCoroutine(new CursedBookEntryProbe().Run());
    }
    static void Tick()
    {
        if (EditorApplication.isPlaying && SessionState.GetBool("BOOK_ENTRY_QA",false)) StartProbe();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        // A play-mode exit can still be pending when delayCall fires. Resume on the next idle tick.
        if (SessionState.GetBool("BOOK_ENTRY_QA",false) &&
            SessionState.GetInt("BOOK_ENTRY_RATIO",0)>0 &&
            !SessionState.GetBool("BOOK_ENTRY_FAILED",false)) { Begin(); return; }
        string request = CursedBookIntroBuilder.Output + "entry-validate.request";
        if (!File.Exists(request)) return;
        int startRatio; if (!int.TryParse(File.ReadAllText(request), out startRatio)) startRatio=0;
        startRatio=Mathf.Clamp(startRatio,0,2);
        File.Delete(request); Directory.CreateDirectory(Dir);
        if(startRatio==0) File.WriteAllText(Dir + "validation.txt", "Live home page entry / actual Unity render / 3 aspect ratios\n");
        else File.AppendAllText(Dir + "validation.txt", "\nRETRY ratio index "+startRatio+" after shared-editor interruption\n");
        SessionState.SetInt("BOOK_ENTRY_RATIO", startRatio); SessionState.SetBool("BOOK_ENTRY_FAILED", false);
        SessionState.SetBool("BOOK_ENTRY_QA", true); Begin();
    }
    static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int i=SessionState.GetInt("BOOK_ENTRY_RATIO",0);
        SessionState.SetBool("BOOK_ENTRY_RUNNING",false);
        Resolution(new[]{720,768,720}[i],new[]{1280,1024,1440}[i]);
        SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
        if(!SessionState.GetBool("BOOK_ENTRY_LOCKED",false)) {
            EditorApplication.LockReloadAssemblies(); SessionState.SetBool("BOOK_ENTRY_LOCKED",true);
        }
        EditorApplication.isPlaying=true;
    }
    public static void Log(string s) { File.AppendAllText(Dir+"validation.txt",s+"\n"); }
    static void Resolution(int w,int h)
    {
        var asm=typeof(Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");var instance=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var group=type.GetMethod("GetGroup").Invoke(instance,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
        var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),w,h,"Book entry "+w+"x"+h},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        var view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1);view.Focus();view.Repaint();
    }
}

public sealed class CursedBookEntryProbe
{
    string ratio; int index, errors; float deadline; bool failed;
    Texture2D finalPage;
    readonly float[] times = {.4f,1.7f,3.4f,5.4f,6.5f};
    void OnLog(string m,string s,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!m.Contains("Ad is unavailable")){errors++;CursedBookIntroValidation.Log("ERROR "+m);}}
    void Check(bool pass,string s){CursedBookIntroValidation.Log((pass?"PASS ":"FAIL ")+ratio+" "+s);if(!pass){failed=true;SessionState.SetBool("BOOK_ENTRY_FAILED",true);}}
    Texture2D Shot(string name)
    {
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(CursedBookIntroValidation.Dir+ratio+"-"+name+".png",image.EncodeToPNG());
        return image;
    }
    public IEnumerator Run()
    {
        Application.logMessageReceived += OnLog;
        index=SessionState.GetInt("BOOK_ENTRY_RATIO",0);ratio=new[]{"9x16","3x4","1x2"}[index];
        deadline=Time.realtimeSinceStartup+90;
        var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        Check(intro&&intro.gameObject.activeSelf,"startup prefab active");
        if(!intro){Finish();yield break;}
        Check(!GameObject.Find("Intro (KEEP ON)"),"legacy shark disabled");
        Vector3 lockedRoomScale=Vector3.zero; Vector2 lockedRoomPosition=Vector2.zero; bool framingStable=true;
        int capture=0,videoFrame=0;float nextVideo=0;bool held=false,released=false,atSeam=false,stationaryChecked=false,coverChecked=false;
        float[] closedCoverVertices=null;
        bool openingChecked=false, pageTurnChecked=false;
        if(index==0)Directory.CreateDirectory(CursedBookIntroValidation.Dir+"frames");
        var readyField=typeof(CursedBookIntro).GetField("<HomeReady>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        while(!intro.Finished && Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();
            if(!coverChecked && intro.Elapsed<1) {
                coverChecked=true;
                var center=((RectTransform)intro.transform).InverseTransformPoint(intro.homePage.rectTransform.TransformPoint(Vector3.zero));
                Check(((Vector2)center).magnitude<1,"initial closed cover centered on screen");
                var backdrop=intro.transform.Find("IntroBackdropCamera").GetComponent<Camera>();
                Check(backdrop.enabled&&backdrop.targetTexture==null&&backdrop.cullingMask==0,"startup display has dedicated background camera");
                UnityEngine.Object.Destroy(Shot("centered-cover"));
                var slot=intro.book.Skeleton.FindSlot("front_board");
                var mesh=(Spine.VertexAttachment)slot.Attachment;
                closedCoverVertices=new float[mesh.WorldVerticesLength];
                mesh.ComputeWorldVertices(slot,0,mesh.WorldVerticesLength,closedCoverVertices,0,2);
            }
            if(!openingChecked && intro.Elapsed>=1.7f && !intro.AnimationFinished) {
                openingChecked=true;
                var slot=intro.book.Skeleton.FindSlot("front_board");
                var mesh=(Spine.VertexAttachment)slot.Attachment;
                var vertices=new float[mesh.WorldVerticesLength];
                mesh.ComputeWorldVertices(slot,0,mesh.WorldVerticesLength,vertices,0,2);
                float movement=0, minX=float.PositiveInfinity, maxX=float.NegativeInfinity;
                if(closedCoverVertices!=null) for(int v=0;v<vertices.Length;v++) {
                    movement=Mathf.Max(movement,Mathf.Abs(vertices[v]-closedCoverVertices[v]));
                    if(v%2==0) { minX=Mathf.Min(minX,closedCoverVertices[v]);maxX=Mathf.Max(maxX,closedCoverVertices[v]); }
                }
                // SkeletonData import scale is independent of the canvas scale: measure relative to cover width.
                float coverWidth=maxX-minX;
                Check(coverWidth>0 && movement>coverWidth*.25f,"cover mesh visibly opens (local movement="+movement.ToString("F2")+", cover width="+coverWidth.ToString("F2")+")");
                Check(intro.book.AnimationState.GetCurrent(0).MixingFrom==null,"manual opening track has no frozen crossfade");
            }
            if(!pageTurnChecked && intro.Elapsed>=2.5f && intro.Elapsed<2.8f) {
                pageTurnChecked=true;
                var page=intro.book.Skeleton.FindSlot("turning_page_0");
                Check(page.A>.5f && page.Deform.Count>0,"turning paper is visible and deformed during opening");
                UnityEngine.Object.Destroy(Shot("paper-turn"));
            }
            if(index==0 && intro.Elapsed>=nextVideo && !intro.Finished) {
                var frame=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(CursedBookIntroValidation.Dir+"frames/frame-"+(videoFrame++).ToString("D4")+".png",frame.EncodeToPNG());
                UnityEngine.Object.Destroy(frame);nextVideo+=.1f;
            }
            if(intro.HomeReady&&!held&&index==0) { readyField.SetValue(intro,false);held=true; }
            if(held&&!released&&intro.AnimationFinished&&Time.realtimeSinceStartup>6.2f) {
                Check(intro.RevealProgress==0,"slow readiness holds on blank book instead of entering unfinished home");
                readyField.SetValue(intro,true);released=true;
            }
            if(capture<times.Length && intro.Elapsed>=times[capture]){UnityEngine.Object.Destroy(Shot("stage-"+capture));capture++;}
            if (intro.RevealProgress == 1 && intro.PushProgress == 0 && !stationaryChecked) {
                stationaryChecked = true;
                var lockedRoom=GameplayCanvas.single.homeUI.GetComponent<ShelterResponsiveLayout>().room;
                lockedRoomScale=lockedRoom.localScale; lockedRoomPosition=lockedRoom.anchoredPosition;
                Check(GameplayCanvas.single.homeUI.GetComponent<ShelterResponsiveLayout>().presentationControls.alpha==0,"book page contains scene only, controls hidden");
                Check(Mathf.Abs(intro.homePage.rectTransform.rect.width/intro.homePage.rectTransform.rect.height-.75f)<.001f,"book illustration retains fixed 3:4 composition");
                var pageCorners = new Vector3[4]; intro.homePage.rectTransform.GetWorldCorners(pageCorners);
                float pageFraction = (pageCorners[1].y-pageCorners[0].y)/Screen.height;
                Check(Mathf.Abs(pageFraction-intro.pageHoldViewportFraction)<.002f,"stationary reveal framing matches reference page height");
                Check(intro.RevealProgress==1 && intro.PushProgress==0,"complete ink reveal precedes final push");
                var center = ((RectTransform)intro.transform).InverseTransformPoint(intro.homePage.rectTransform.TransformPoint(Vector3.zero));
                Check(((Vector2)center).magnitude<1,"right page stays at screen center before push");
                UnityEngine.Object.Destroy(Shot("revealed-before-push"));
            }
            if(stationaryChecked && !intro.Finished) {
                var room=GameplayCanvas.single.homeUI.GetComponent<ShelterResponsiveLayout>().room;
                framingStable &= Vector3.Distance(room.localScale,lockedRoomScale)<.001f && Vector2.Distance(room.anchoredPosition,lockedRoomPosition)<.01f;
            }
            if(intro.PushProgress>=1&&intro.ControlsProgress>=1&&!atSeam) {
                atSeam=true;
                foreach(var sg in GameplayCanvas.single.homeUI.GetComponentsInChildren<SkeletonGraphic>())sg.timeScale=0;
                Check(intro.HomeReady && intro.HomeTexture && intro.RevealProgress==1,"real home texture completely visible before handoff");
                var r=intro.homePage.rectTransform;var corners=new Vector3[4];r.GetWorldCorners(corners);
                var root=(RectTransform)intro.transform;
                var a=root.InverseTransformPoint(corners[0]);var b=root.InverseTransformPoint(corners[2]);
                Check(Mathf.Abs(a.y-root.rect.yMin)<1 && Mathf.Abs(b.y-root.rect.yMax)<1 && a.x<=root.rect.xMin+1 && b.x>=root.rect.xMax-1,"illustration fills viewport without stretching");
                finalPage=Shot("full-page");
            }
        }
        Check(framingStable,"room scale and position remain fixed during camera push and UI reveal");
        Check(stationaryChecked,"stationary fully revealed stage observed");
        Check(coverChecked,"initial cover frame observed");
        Check(openingChecked && pageTurnChecked,"actual cover and paper animation frames observed");
        Check(intro.Finished,"handoff completes before timeout");
        yield return new WaitForEndOfFrame();
        var home=Shot("live-home");
        if(index==0) {
            for(int f=0;f<8;f++)File.WriteAllBytes(CursedBookIntroValidation.Dir+"frames/frame-"+(videoFrame++).ToString("D4")+".png",home.EncodeToPNG());
            File.WriteAllText(CursedBookIntroValidation.Dir+"frames/count.txt",videoFrame.ToString());
        }
        if(finalPage){
            Check(home.width==finalPage.width && home.height==finalPage.height,"boundary screenshots have equal dimensions");
            var aa=finalPage.GetPixels32();var bb=home.GetPixels32();double error=0;int changed=0;
            if(aa.Length==bb.Length)for(int i=0;i<aa.Length;i++){
                int delta=Math.Abs(aa[i].r-bb[i].r)+Math.Abs(aa[i].g-bb[i].g)+Math.Abs(aa[i].b-bb[i].b);error+=delta;
                if(delta>30)changed++;
            }
            double mae=error/(aa.Length*3.0*255),fraction=(double)changed/aa.Length;
            CursedBookIntroValidation.Log(ratio+" seam MAE="+mae.ToString("F6")+" changed>10/channel="+fraction.ToString("P3"));
            Check(mae<.025 && fraction<.08,"page-to-home seam has no framing jump or black flash");
            UnityEngine.Object.Destroy(finalPage);
        }else Check(false,"captured full-screen page before handoff");
        UnityEngine.Object.Destroy(home);
        var ui=GameplayCanvas.single.homeUI;
        Check(ui.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"live home restored to overlay");
        Check(ui.GetComponent<GraphicRaycaster>().enabled,"live buttons enabled after handoff");
        Check(!intro.gameObject.activeSelf && !intro.group.blocksRaycasts && intro.HomeTexture==null,"intro hides and releases capture texture");
        Check(ui.GetComponentsInChildren<Transform>(true).All(t=>t.gameObject.layer!=31),"temporary capture layers restored");
        Check(GameManager.single.gameState==GameStateType.CAMP,"camp state retained");
        if(index==2 && !failed){
            foreach(var sg in ui.GetComponentsInChildren<SkeletonGraphic>())sg.timeScale=1;
            ui.battleButton.onClick.Invoke();yield return new WaitForSecondsRealtime(3);
            Check(GameManager.single.gameState==GameStateType.BATTLE,"battle button works after seamless entry");
            GameplayCanvas.single.battleHud.backButton.onClick.Invoke();yield return new WaitForSecondsRealtime(3);
            Check(GameManager.single.gameState==GameStateType.CAMP,"battle return retained");
        }
        Check(errors==0,"no unexpected runtime errors");Finish();
    }
    void Finish(){Application.logMessageReceived -= OnLog;CursedBookIntroValidation.Log((failed?"FAIL ":"PASS ")+ratio+" COMPLETE");EditorApplication.isPlaying=false;}
}

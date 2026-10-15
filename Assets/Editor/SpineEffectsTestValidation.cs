using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SpineEffectsTestValidation
{
    const string Out="output/spine-effects-test/";
    static bool active;static int phase,index,ratio,errors;static double next;
    static int[] widths={720,768,720},heights={1280,1024,1440};
    static SpineEffectsTestValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=(s,t,k)=>{if(active&&(k==LogType.Error||k==LogType.Exception)){errors++;File.AppendAllText(Out+"validation.txt",s+"\n");}};}
    public static void Begin(){if(EditorApplication.isPlaying)throw new Exception("Stop Play first");Directory.CreateDirectory(Out);EditorSceneManager.OpenScene(SpineEffectsTestBuilder.ScenePath);SessionState.SetBool("FX_TEST_VALIDATE",true);EditorApplication.isPlaying=true;}
    static void Check(bool condition,string text){if(!condition)throw new Exception(text);File.AppendAllText(Out+"validation.txt","PASS "+text+"\n");}
    static void Resolution(){typeof(MonsterPreviewValidation).GetMethod("SetResolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{widths[ratio],heights[ratio]});}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlaying&&SessionState.GetBool("FX_TEST_VALIDATE",false)){SessionState.SetBool("FX_TEST_VALIDATE",false);active=true;phase=index=ratio=errors=0;next=EditorApplication.timeSinceStartup+2;File.WriteAllText(Out+"validation.txt","Spine effects test scene\n");Resolution();}
        if(!active||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        try {
            var t=UnityEngine.Object.FindFirstObjectByType<SpineEffectsTest>();Check(t!=null,"test controller active");
            if(phase==0){t.selectionButtons[index].onClick.Invoke();t.widthSlider.value=25;t.Seek(index<5?t.Duration*.7f:(index==8||index==11?.45f:.12f));Check(t.Current?t.Current.skeleton.GetComponent<MeshFilter>().sharedMesh.vertexCount>0:System.Array.Exists(t.CurrentParticle.GetComponentsInChildren<ParticleSystem>(),p=>p.particleCount>0),"visible geometry "+index);phase=1;next=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==1){ScreenCapture.CaptureScreenshot(Out+"effect-"+index+".png");phase=6;next=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==6){t.widthSlider.value=40;Check(Mathf.Abs(t.Visual.localScale.x*t.MotionBounds.size.x-4)<.001f,"width control "+index);t.speedSlider.value=.5f;t.replayButton.onClick.Invoke();phase=2;next=EditorApplication.timeSinceStartup+.2;return;}
            if(phase==2){Check(t.TimePosition>0,"playback "+index);t.pauseButton.onClick.Invoke();Check(t.Paused,"pause "+index);float before=t.TimePosition;t.stepButton.onClick.Invoke();Check(t.TimePosition>before,"step "+index);t.loopButton.onClick.Invoke();Check(!t.Loop,"loop off");t.loopButton.onClick.Invoke();if(++index<t.Count){phase=0;return;}t.widthSlider.value=25;t.speedSlider.value=1;t.Select(4);t.Seek(.65f);phase=3;return;}
            if(phase==3){Resolution();phase=4;next=EditorApplication.timeSinceStartup+.6;return;}
            if(phase==4){Canvas.ForceUpdateCanvases();foreach(var slider in new Component[]{t.timeSlider,t.widthSlider,t.speedSlider,t.selectionButtons[0],t.selectionButtons[11],t.replayButton,t.backgroundButton}){var corners=new Vector3[4];slider.GetComponent<RectTransform>().GetWorldCorners(corners);Check(corners[0].x>=0&&corners[2].x<=Screen.width+1&&corners[0].y>=0&&corners[2].y<=Screen.height+1,"slider in viewport "+widths[ratio]+"x"+heights[ratio]);}ScreenCapture.CaptureScreenshot(Out+"layout-"+widths[ratio]+"x"+heights[ratio]+".png");if(++ratio<3){phase=3;return;}phase=5;next=EditorApplication.timeSinceStartup+.5;return;}
            if(phase==5){t.backgroundButton.onClick.Invoke();Check(!t.backdrop.gameObject.activeSelf,"plain background");t.backgroundButton.onClick.Invoke();Check(t.backdrop.gameObject.activeSelf,"battle background");ratio=0;Resolution();t.Select(0);t.widthSlider.value=25;t.Replay();Check(errors==0,"no runtime errors");File.AppendAllText(Out+"validation.txt","COMPLETE\n");active=false;}
        }catch(Exception e){File.AppendAllText(Out+"validation.txt","FAIL "+e+"\n");active=false;Debug.LogException(e);}
    }
}

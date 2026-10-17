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
    static int resFrame;static bool active;static int phase,index,ratio,errors;static double next;
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
            if(phase==0){t.selectionButtons[index].onClick.Invoke();t.widthSlider.value=100;t.Seek(index<5?t.Duration*(index==2?.3f:.7f):(index==5?.0667f:index==7?1.16f:index==8?.4f:index==11?.45f:.12f));Check(t.Current?t.Current.skeleton.GetComponent<MeshFilter>().sharedMesh.vertexCount>0:t.CommonSpine?t.CommonSpine.skeleton.GetComponent<MeshFilter>().sharedMesh.vertexCount>0:System.Array.Exists(t.CurrentParticle.GetComponentsInChildren<ParticleSystem>(),p=>p.particleCount>0),"visible geometry "+index);phase=1;next=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==1){if(index<5)Check(Mathf.Abs(t.Visual.localScale.x*t.MotionBounds.size.x-t.petBattleEffects.EffectWorldWidth(t.effects[index],t.previewCamera))<.001f,"pet width equals battle config "+index);ScreenCapture.CaptureScreenshot(Out+"effect-"+index+".png");phase=6;next=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==6){if(index==7){
                t.Seek(.3f);Check(t.hero.AnimationState.GetCurrent(0).Animation.Name=="Skill5","sweep uses Skill5");
                Check(!t.CastPreview.gameObject.activeSelf&&!t.CommonSpine.skeleton.gameObject.activeSelf,"windup before effects");
                t.Seek(t.sweepHitTime+.02f);Check(t.CastPreview.gameObject.activeSelf&&t.CommonSpine.skeleton.gameObject.activeSelf,"slash and impact aligned");
                t.Seek(t.sweepCastTime-.01f);Check(!t.CastPreview.gameObject.activeSelf&&!t.CommonSpine.skeleton.gameObject.activeSelf,"no effects before downstroke");t.Seek(t.sweepHitTime-.01f);Check(!t.CommonSpine.skeleton.gameObject.activeSelf,"main effect waits for delayed hit");t.Seek(t.sweepHitTime+.02f);
                float pose=t.hero.Skeleton.FindBone("weapon").Rotation;t.Seek(.2f);t.Seek(t.sweepHitTime+.02f);Check(Mathf.Abs(pose-t.hero.Skeleton.FindBone("weapon").Rotation)<.001f,"scrub reconstructs same hero pose");
                t.Seek(1.5f);Check(t.hero.AnimationState.GetCurrent(0).Animation.Name=="Idle1","recover to idle");
                t.Seek(1.1f);var origin=t.effectAnchor.position;var enemyPos=t.enemy.transform.position;t.enemy.transform.position+=Vector3.right*2;t.Seek(1.1f);Check(Vector3.Distance(origin,t.effectAnchor.position)<.001f,"sweep ignores enemy position");t.enemy.transform.position=enemyPos;
                t.Seek(t.sweepHitTime+t.sweepFlyDelay+.17f);Check(t.effectAnchor.position.x>origin.x+1,"blade travels right");t.Seek(2.2f);Check(!t.CommonSpine.skeleton.gameObject.activeSelf,"blade exits screen");
            }t.widthSlider.value=150;Check(t.CurrentParticle?Vector3.Distance(t.Visual.localScale,t.particleEffects[index-t.effects.Length].transform.localScale*1.5f)<.001f:Mathf.Abs(t.Visual.localScale.x*t.MotionBounds.size.x-t.BaseWidth*1.5f)<.001f,"width control "+index);t.speedSlider.value=.5f;t.replayButton.onClick.Invoke();phase=2;next=EditorApplication.timeSinceStartup+.2;return;}
            if(phase==2){Check(t.TimePosition>0,"playback "+index);t.pauseButton.onClick.Invoke();Check(t.Paused,"pause "+index);float before=t.TimePosition;t.stepButton.onClick.Invoke();Check(t.TimePosition>before,"step "+index);t.loopButton.onClick.Invoke();Check(!t.Loop,"loop off");t.loopButton.onClick.Invoke();if(++index<t.Count){phase=0;return;}t.widthSlider.value=100;t.speedSlider.value=1;t.Select(4);t.Seek(.65f);phase=3;return;}
            if(phase==3){Resolution();phase=4;next=EditorApplication.timeSinceStartup+.6;return;}
            if(phase==4){Canvas.ForceUpdateCanvases();var viewport=new Vector3[4];t.safeRoot.GetComponentInParent<Canvas>().GetComponent<RectTransform>().GetWorldCorners(viewport);Check(Mathf.Abs((viewport[2].x-viewport[0].x)/(viewport[2].y-viewport[0].y)-(float)widths[ratio]/heights[ratio])<.005f,"rendered canvas aspect ratio");foreach(var slider in new Component[]{t.timeSlider,t.widthSlider,t.speedSlider,t.selectionButtons[0],t.selectionButtons[11],t.replayButton,t.backgroundButton}){var corners=new Vector3[4];slider.GetComponent<RectTransform>().GetWorldCorners(corners);Check(corners[0].x>=viewport[0].x-2&&corners[2].x<=viewport[2].x+2&&corners[0].y>=viewport[0].y-2&&corners[2].y<=viewport[2].y+2,"control in viewport "+slider.name+" "+Screen.width+"x"+Screen.height+" "+corners[0]+" / "+corners[2]);}ScreenCapture.CaptureScreenshot(Out+"layout-"+widths[ratio]+"x"+heights[ratio]+".png");index=0;phase=8;return;}
            if(phase==8){resFrame=0;t.Select(index);t.Seek(index<5?t.Duration*(index==2?.3f:.7f):index==5?.0667f:index==7?1.16f:index==8?.4f:index==11?.45f:.15f);Check(t.hero&&t.enemy,"preview actors present");if(index<5)Check(Mathf.Abs(t.Visual.localScale.x*t.MotionBounds.size.x-t.petBattleEffects.EffectWorldWidth(t.effects[index],t.previewCamera))<.001f,"pet battle size at "+widths[ratio]+"x"+heights[ratio]+" index "+index);phase=9;next=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==9){ScreenCapture.CaptureScreenshot(Out+"actors-"+index+"-"+widths[ratio]+"x"+heights[ratio]+(resFrame==0?"":"-rise-"+resFrame)+".png");if(index==7&&resFrame<4){phase=11;next=EditorApplication.timeSinceStartup+.3;return;}if(index==9&&resFrame<4){phase=10;next=EditorApplication.timeSinceStartup+.3;return;}if(++index<t.Count){phase=8;return;}if(++ratio<3){phase=3;return;}phase=5;next=EditorApplication.timeSinceStartup+.5;return;}
            if(phase==11){resFrame++;t.Seek(new float[]{0,t.sweepHitTime+.02f,t.sweepHitTime+.29f,t.sweepHitTime+t.sweepFlyDelay+.17f,2.2f}[resFrame]);if(resFrame==1)Check(t.CastPreview&&t.CastPreview.skeleton.GetComponent<MeshFilter>().sharedMesh.vertexCount>0,"cast preview visible");phase=9;next=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==10){resFrame++;t.Seek(new float[]{0,.4f,1.1f,1.5f,1.85f}[resFrame]);phase=9;next=EditorApplication.timeSinceStartup+.3;return;}
            if(phase==5){t.backgroundButton.onClick.Invoke();Check(!t.backdrop.gameObject.activeSelf,"plain background");t.backgroundButton.onClick.Invoke();Check(t.backdrop.gameObject.activeSelf,"battle background");ratio=0;Resolution();t.Select(0);t.widthSlider.value=100;t.Replay();Check(errors==0,"no runtime errors");File.AppendAllText(Out+"validation.txt","COMPLETE\n");active=false;}
        }catch(Exception e){File.AppendAllText(Out+"validation.txt","FAIL "+e+"\n");active=false;Debug.LogException(e);}
    }
}

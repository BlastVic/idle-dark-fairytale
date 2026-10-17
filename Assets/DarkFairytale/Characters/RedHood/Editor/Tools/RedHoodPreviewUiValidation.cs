using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RedHoodPrototype.Editor {
 [InitializeOnLoad] public static class RedHoodPreviewUiValidation {
  const string Key="RED_HUD_QA";const string Out=RedHoodPreviewUiBuilder.Out;
  static int phase,ratio;static double next,start;
  static readonly int[] W={720,768,720},H={1280,1024,1440};
  static RedHoodPreviewUiValidation(){EditorApplication.update+=Tick;}
  static void Check(bool ok,string msg){File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+msg+"\n");if(!ok)throw new Exception(msg);}
  static void Size(){typeof(MonsterPreviewValidation).GetMethod("SetResolution",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{W[ratio],H[ratio]});}
  static void Later(int p,float delay=.4f){phase=p;next=EditorApplication.timeSinceStartup+delay;}
  static void Shot(string name){ScreenCapture.CaptureScreenshot(Out+name+"-"+W[ratio]+"x"+H[ratio]+".png");}
  static void Tick(){
   if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   if(phase==0&&!SessionState.GetBool(Key,false)&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){
    File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");EditorSceneManager.OpenScene(RedHoodPreviewUiBuilder.ScenePath);
    var authored=UnityEngine.Object.FindFirstObjectByType<RedHoodAnimationPreview>();
    Check(authored.hud,"Persisted HUD reference");
    Check(PrefabUtility.IsPartOfPrefabInstance(authored.hud),"Editable UGUI prefab instance");
    SessionState.SetBool(Key,true);ratio=0;Size();start=EditorApplication.timeSinceStartup;EditorApplication.isPlaying=true;return;
   }
   if(!SessionState.GetBool(Key,false))return;
   if(start==0)start=EditorApplication.timeSinceStartup;
   if(!EditorApplication.isPlaying)return;
   try{
    if(EditorApplication.timeSinceStartup-start>120)throw new Exception("Timeout");
    var m=UnityEngine.Object.FindFirstObjectByType<RedHoodAnimationPreview>();if(!m||m.AnimationNames.Length==0)return;var h=m.hud;
    if(EditorApplication.timeSinceStartup<next)return;
    if(phase==0){
     Check(h.animationButtons.Length==m.AnimationNames.Length,"All production animations selectable");
     foreach(var b in h.animationButtons){b.onClick.Invoke();Check(m.Selected==b.name&&m.Playing==b.name,"Select "+b.name);}
     h.selector.onClick.Invoke();Check(h.animationPanel.activeSelf,"Catalog expands");h.search.text="skill3";Check(h.animationButtons.Count(b=>b.gameObject.activeSelf)==1,"Search filter");h.search.text="no-result";Check(h.empty.gameObject.activeSelf,"Search empty state");h.search.text="";h.SetCatalog(false);
     Check(!h.catalogGroup.blocksRaycasts&&!h.catalogGroup.interactable,"Collapsed catalog cannot block raycasts");
     m.Select("Skill3");h.pause.onClick.Invoke();Check(m.paused,"Pause");h.timeSlider.value=1;Check(m.paused&&Mathf.Abs(m.Timeline-1)<.001f,"Scrub exact pose");h.nextFrame.onClick.Invoke();Check(Mathf.Abs(m.Timeline-1-1f/60)<.001f,"Step forward 1/60s");h.previousFrame.onClick.Invoke();Check(Mathf.Abs(m.Timeline-1)<.001f,"Step backward 1/60s");
     h.pause.onClick.Invoke();Check(!m.paused,"Resume");h.replay.onClick.Invoke();Check(m.Timeline<.01f,"Replay");
     h.loopToggle.SetIsOnWithoutNotify(false);h.loopToggle.isOn=true;Check(m.Current.Loop,"Loop");h.loopToggle.isOn=false;
     h.speedSlider.value=.5f;Check(m.speed==.5f,"Speed slider");h.speedSlider.value=1;
     h.zoomSlider.value=1.4f;Check(m.zoom==1.4f,"Zoom slider");h.reset.onClick.Invoke();Check(m.zoom==1,"Reset view");
     m.Select("Death1");Check(Mathf.Abs(m.Current.TimeScale-1.2f)<.001f,"Death matches 1.2x combat speed");
     m.Select("Attack1");m.character.Update(1.3f);Check(m.HitCount==1&&m.CompleteCount==1,"Events counted once");
     Later(1,1f);return;
    }
    if(phase==1){Check(m.Playing=="Idle1","Attack returns to Idle");h.attackToggle.isOn=true;Later(2,1.7f);return;}
    if(phase==2){Check(m.Playing=="Attack2","Auto combo alternates attacks");h.attackToggle.isOn=false;m.Select("Idle1");h.cycleToggle.isOn=true;Later(3,3.9f);return;}
    if(phase==3){Check(m.Selected!="Idle1","Cycle leaves Idle");h.cycleToggle.isOn=false;m.Select("Skill3");m.Seek(2.5f);Later(4);return;}
    if(phase==4){
     Canvas.ForceUpdateCanvases();var bounds=m.character.GetComponent<Renderer>().bounds;
     var min=m.previewCamera.WorldToScreenPoint(bounds.min);var max=m.previewCamera.WorldToScreenPoint(bounds.max);
     var c=new Vector3[4];h.previewArea.GetWorldCorners(c);
     Check(min.x>=c[0].x-2&&max.x<=c[2].x+2&&min.y>=c[0].y-2&&max.y<=c[2].y+2,"Full overhead pose inside stage "+W[ratio]+"x"+H[ratio]);
     foreach(var button in new[]{h.pause,h.replay,h.nextFrame,h.reset}){var r=(RectTransform)button.transform;r.GetWorldCorners(c);Check(c[0].x>=0&&c[2].x<=Screen.width+1&&c[0].y>=0&&c[2].y<=Screen.height+1,"Control visible "+button.name);}
     Shot("closed");Later(5);return;
    }
    if(phase==5){h.SetCatalog(true);Later(6);return;}
    if(phase==6){Check(h.catalogGroup.alpha==1,"Catalog expansion completes");Shot("catalog");Later(60);return;}
    if(phase==60){h.search.text="no-result";Later(7);return;}
    if(phase==7){Shot("empty");Later(70);return;}
    if(phase==70){h.search.text="";h.SetCatalog(false);m.Select("Idle1");m.Seek(0);Later(8);return;}
    if(phase==8){Check(!h.previousFrame.interactable,"Frame start disabled");Check(!h.animationPanel.activeInHierarchy,"Catalog collapse completes");Shot("disabled");Later(9);return;}
    if(phase==9){if(++ratio<3){Size();m.Select("Skill3");m.Seek(2.5f);Later(4,.8f);}else{m.Select("Idle1");File.AppendAllText(Out+"validation.txt","COMPLETE\n");SessionState.SetBool(Key,false);phase=0;EditorApplication.isPlaying=false;}return;}
   }catch(Exception e){File.AppendAllText(Out+"validation.txt","FAIL "+e+"\n");SessionState.SetBool(Key,false);phase=0;EditorApplication.isPlaying=false;Debug.LogException(e);}
  }
 }
}

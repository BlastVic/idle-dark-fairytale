using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MobRivenfangBloodProbe {
 static MonsterAnimationTest t;static MonsterIdleBlood fx;static int step,emitted;static double until;static Vector3 position;static Transform drop;
 const string Report="output/monster-animation/MobRivenfang/blood-validation.txt";
 public static void Begin(){
  if(!EditorApplication.isPlaying)throw new Exception("Enter test scene Play mode");
  t=UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();t.RefreshCatalog();
  t.SelectMonster(Array.FindIndex(t.monsters,d=>d.name=="MobRivenfang_SkeletonData"));t.SetAutoCycle(false);t.SetPaused(false);t.speed=1;t.Play(0);
  fx=t.character.GetComponent<MonsterIdleBlood>();if(!fx)throw new Exception("Preview did not copy authored blood effect");
  File.WriteAllText(Report,"MobRivenfang live Idle blood regression\n");step=1;until=EditorApplication.timeSinceStartup+fx.interval+.25;EditorApplication.update+=Tick;
 }
 static void CaptureDetail(){
  var go=new GameObject("Blood detail camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(t.previewCamera);camera.orthographicSize=.4f;camera.rect=new Rect(0,0,1,1);camera.transform.position=new Vector3(fx.LastEmission.x,fx.LastEmission.y-.08f,-20);
  var rt=new RenderTexture(512,512,24);var image=new Texture2D(512,512,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes("output/monster-animation/MobRivenfang/blood-detail.png",image.EncodeToPNG());}
  finally {RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
 }
 static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);File.AppendAllText(Report,"PASS "+msg+"\n");}
 static void Next(int n,double wait){step=n;until=EditorApplication.timeSinceStartup+wait;}
 static void Tick(){try {
  if(!EditorApplication.isPlaying)throw new Exception("Play stopped");
  if(EditorApplication.timeSinceStartup<until)return;
  if(step==1){Check(fx.EmittedCount>=1 && fx.ActiveCount>0,"Idle emits visible blood");Check(fx.TryGetTip(out var tip),"Skinned blade endpoint available");
   drop=fx.transform.Cast<Transform>().First(x=>x.name=="Idle blood drop"&&x.gameObject.activeSelf);position=drop.position;
   Check(Vector3.Distance(position,tip)<.6f,"Emitter remains near bloody blade tip");ScreenCapture.CaptureScreenshot("output/monster-animation/MobRivenfang/idle-blood.png");CaptureDetail();Next(2,.1);}
  else if(step==2){Check(drop.position.y<position.y,"Blood falls downward with mirrored character");t.SetPaused(true);position=drop.position;emitted=fx.EmittedCount;Next(3,.15);}
  else if(step==3){Check(fx.EmittedCount==emitted && (drop.position-position).sqrMagnitude<.00001f,"Pause freezes particle motion and emission");t.SetPaused(false);t.Play(1);Next(4,1.1);}
  else if(step==4){Check(fx.EmittedCount==emitted,"Attack does not emit Idle blood");Check(fx.ActiveCount==0,"Detached blood fades and is pooled");t.Play(0);Next(5,fx.interval+.25);}
  else if(step==5){Check(fx.EmittedCount>emitted,"Returning to Idle resumes emission");t.Play(3);Next(6,.1);}
  else if(step==6){Check(fx.ActiveCount==0,"Death clears particles");t.Play(0);File.AppendAllText(Report,"PASS all live blood checks\n");EditorApplication.update-=Tick;step=0;}
 }catch(Exception e){File.AppendAllText(Report,"FAIL "+e+"\n");EditorApplication.update-=Tick;step=0;Debug.LogException(e);}}
}

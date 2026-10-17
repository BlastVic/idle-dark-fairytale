using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Game.Enum;

/// <summary>Opt-in Play Mode visual regression. Captures stay in ignored output/.</summary>
[InitializeOnLoad]
public static class ShelterIdleValidation
{
    const string Dir="output/home-refined/";
    static int stage,frame,ratio,errors;
    static double due,deadline;
    static SkeletonGraphic graphic, fire;
    static float playbackTime;
    static Vector3 originalScale; static Vector2 originalPosition;
    static readonly int[] Width={720,768,720},Height={1280,1024,1440};
    static readonly string[] Ratio={"9x16","3x4","1x2"};
    static ShelterIdleValidation(){
        if(SessionState.GetBool("SHELTER_IDLE_PENDING",false)){SessionState.SetBool("SHELTER_IDLE_PENDING",false);stage=1;due=EditorApplication.timeSinceStartup+3;deadline=due+180;}
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(stage>0&&(t==LogType.Exception||t==LogType.Error)&&!m.Contains("Ad is unavailable")){errors++;Log("ERROR "+m);}};
    }
    static void Log(string text){File.AppendAllText(Dir+"validation.txt",text+"\n");}
    static void Check(bool pass,string name){if(!pass)throw new Exception(name);Log("PASS "+name);}
    static void Next(int s,double delay=.3){stage=s;due=EditorApplication.timeSinceStartup+delay;}
    static void Pose(float t){
        graphic.Skeleton.SetToSetupPose();
        graphic.Skeleton.Data.FindAnimation("Idle").Apply(graphic.Skeleton,0,t,false,null,1,MixBlend.Setup,MixDirection.In);
        graphic.Skeleton.UpdateWorldTransform();graphic.UpdateMesh();
        if(fire){fire.Skeleton.SetToSetupPose();fire.Skeleton.Data.FindAnimation("Burn").Apply(fire.Skeleton,0,t%8,false,null,1,MixBlend.Setup,MixDirection.In);fire.Skeleton.UpdateWorldTransform();fire.UpdateMesh();}
    }
    static void Shot(string name){ScreenCapture.CaptureScreenshot(Dir+name+".png");}
    static float[] WorldVertices(Skeleton skeleton){
        var slot=skeleton.FindSlot("seated_character");var mesh=(MeshAttachment)slot.Attachment;
        var values=new float[mesh.WorldVerticesLength];mesh.ComputeWorldVertices(slot,0,values.Length,values,0,2);return values;
    }
    static void Audit()
    {
        Check(fire.startingAnimation=="Burn"&&fire.startingLoop,"Fire prefab loops Burn");
        var animation=fire.Skeleton.Data.FindAnimation("Burn");
        Check(Mathf.Abs(animation.Duration-8)<.001f,"Fire is an eight-second loop");
        float[] firstFire=null;
        for(int f=0;f<=160;f++){
            fire.Skeleton.SetToSetupPose();animation.Apply(fire.Skeleton,0,f*.05f,false,null,1,MixBlend.Setup,MixDirection.In);fire.Skeleton.UpdateWorldTransform();
            var all=new System.Collections.Generic.List<float>();
            foreach(var slot in fire.Skeleton.Slots)if(slot.Attachment is MeshAttachment m){var values=new float[m.WorldVerticesLength];m.ComputeWorldVertices(slot,0,values.Length,values,0,2);CheckFinite(values);all.AddRange(values);}
            if(f==0)firstFire=all.ToArray();if(f==160)Check(firstFire.Zip(all,(a,b)=>Mathf.Abs(a-b)).Max()<.001f,"Fire mesh loop endpoints continuous");
        }
        Log("PASS 161 fire poses: six meshes finite and loop continuous");
        Check(graphic.IsValid,"Spine 3.8 data loads in actual UI renderer");
        Check(!graphic.raycastTarget,"Character does not intercept UI input");
        Check(graphic.startingLoop&&graphic.startingAnimation=="Idle","Saved prefab starts looping Idle");
        Check(graphic.Skeleton.Bones.Count==20,"20 independently editable bones");
        Pose(0);var first=WorldVertices(graphic.Skeleton);Pose(12);var last=WorldVertices(graphic.Skeleton);
        for(int i=0;i<3;i++){
            var slot=graphic.Skeleton.FindSlot("tea_steam_"+i);var m=(MeshAttachment)slot.Attachment;var v=new float[m.WorldVerticesLength];m.ComputeWorldVertices(slot,0,v.Length,v,0,2);
            Log("Steam "+i+" alpha="+slot.A+" local first="+v[0]+","+v[1]+" last="+v[v.Length-2]+","+v[v.Length-1]+" UV="+m.UVs[0]+","+m.UVs[1]);
        }
        Check(first.Zip(last,(a,b)=>Mathf.Abs(a-b)).Max()<.001f,"Character mesh loop endpoint continuous");
        Pose(1);var moved=WorldVertices(graphic.Skeleton);
        Check(first.Zip(moved,(a,b)=>Mathf.Abs(a-b)).Max()>.005f,"Weighted mesh visibly animates");
        var minX=first.Where((v,i)=>i%2==0).Min()*100;var maxX=first.Where((v,i)=>i%2==0).Max()*100;
        Check(maxX-minX>550&&maxX-minX<600,"Spine mesh scale matches the UI illustration in pixels");
        // Two grid vertices at the boot soles remain pinned across the cycle.
        foreach(int x in new[]{34,45}){
            int index=(61*65+x)*2;
            Check(Mathf.Abs(first[index]-moved[index])<.001f&&Mathf.Abs(first[index+1]-moved[index+1])<.001f,"Boot anchor stable "+x);
        }
        Pose(2.69f);Check(graphic.Skeleton.FindSlot("blink").A>.99f,"Closed eyelid attachment appears during blink");
        Pose(3);Check(graphic.Skeleton.FindSlot("blink").A<.01f,"Open eyes restored after blink");
        for(int f=0;f<=144;f++){
            Pose(f/12f);
            var values=WorldVertices(graphic.Skeleton);CheckFinite(values);
            float alpha=0;for(int j=0;j<3;j++)alpha+=graphic.Skeleton.FindSlot("tea_steam_"+j).A;
            if(alpha<.1f)throw new Exception("All steam vanished at "+f/12f);
        }
        Log("PASS 145 sampled poses: finite mesh, staggered steam never fully disappears");Pose(0);
    }
    static void CheckFinite(float[] values){if(values.Any(v=>float.IsNaN(v)||float.IsInfinity(v)))throw new Exception("Non-finite mesh vertex");}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try{
            if(stage==0){
                if(!File.Exists(Dir+"validate.request"))return;
                File.Delete(Dir+"validate.request");Directory.CreateDirectory(Dir+"frames");File.WriteAllText(Dir+"validation.txt","Seated Spine Idle / actual Unity Play Mode\n");
                errors=frame=ratio=0;SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);Resolution(720,1280);deadline=EditorApplication.timeSinceStartup+180;
                if(!EditorApplication.isPlaying){SessionState.SetBool("SHELTER_IDLE_PENDING",true);EditorApplication.isPlaying=true;}
                Next(1,3);return;
            }
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Idle validation timeout, stage "+stage);
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<due)return;
            var gc=GameplayCanvas.single;if(!gc||!gc.homeUI||!GameManager.single||GameManager.single.gameState!=GameStateType.CAMP)return;
            if(stage==1){
                graphic=gc.homeUI.GetComponentsInChildren<SkeletonGraphic>().Single(s=>s.startingAnimation=="Idle");fire=gc.homeUI.GetComponentsInChildren<SkeletonGraphic>().Single(s=>s.startingAnimation=="Burn");Check(fire.IsValid&&!fire.raycastTarget,"Independent fire Spine loads without intercepting input");Check(fire.Skeleton.Bones.Count==16,"Six flame meshes and nine ember bones");Check(graphic!=null,"Home contains persisted Spine actor");
                originalScale=graphic.rectTransform.localScale;originalPosition=graphic.rectTransform.anchoredPosition;
                playbackTime=graphic.AnimationState.GetCurrent(0).TrackTime;Next(8,.5);return;
            }
            if(stage==8){Check(graphic.AnimationState.GetCurrent(0).TrackTime>playbackTime+.1f,"Idle advances automatically in live Play Mode");graphic.freeze=true;fire.freeze=true;Audit();Next(2);return;}
            if(stage==2){
                var ui=gc.homeUI;var layout=ui.GetComponent<ShelterResponsiveLayout>();layout.Apply();Canvas.ForceUpdateCanvases();
                Check((layout.viewport.rect.size-((RectTransform)ui.transform).rect.size).magnitude<.1f,"Supported ratio uses full viewport "+Ratio[ratio]);
                var chestCorners=new Vector3[4];layout.chests.GetWorldCorners(chestCorners);var attrCorners=new Vector3[4];layout.attributes.GetWorldCorners(attrCorners);
                var head=graphic.rectTransform.TransformPoint(new Vector3(0,1220*graphic.skeletonDataAsset.scale*100,0));var feet=graphic.rectTransform.TransformPoint(new Vector3(0,54*graphic.skeletonDataAsset.scale*100,0));
                Check(head.y<chestCorners[0].y-4,"Hood clears chest rail "+Ratio[ratio]);Check(feet.y>attrCorners[1].y+4,"Boots clear attributes "+Ratio[ratio]);
                foreach(var b in ui.GetComponentsInChildren<UnityEngine.UI.Button>()){var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);var pixel=ui.GetComponent<Canvas>().pixelRect;Check(corners.All(c=>c.x>=-1&&c.y>=-1&&c.x<=pixel.width+1&&c.y<=pixel.height+1),"Control inside "+Ratio[ratio]+" "+b.name);}
                ShelterPointerProbe.Run(ui,Dir+"validation.txt");Pose(0);Shot("home-idle-"+Ratio[ratio]);Next(3,.6);return;
            }
            if(stage==3){
                ratio++;if(ratio<3){Resolution(Width[ratio],Height[ratio]);Next(2,1);return;}
                Resolution(720,1280);Next(4,1);return;
            }
            if(stage==4){
                Pose(2.69f);Shot("blink-closed");Next(6,.6);return;
            }
            if(stage==6){
                var rt=graphic.rectTransform;rt.localScale=Vector3.one*2.4f;rt.anchoredPosition=new Vector2(470,-1720);
                Pose(1.5f);Shot("detail");Next(9,.6);return;
            }
            if(stage==9){for(int i=0;i<3;i++)graphic.Skeleton.FindSlot("tea_steam_"+i).A=0;graphic.UpdateMesh();Shot("detail-without-steam");Next(7,.6);return;}
            if(stage==7){graphic.rectTransform.localScale=originalScale;graphic.rectTransform.anchoredPosition=originalPosition;Next(5,.6);return;}
            if(stage==5){
                // Deterministic animation time with real Unity screen renders, 20 fps.
                if(frame<240){Pose(frame/20f);Shot("frames/idle-"+frame.ToString("D4"));frame++;due=EditorApplication.timeSinceStartup+.075;return;}
                graphic.freeze=false;fire.freeze=false;Check(errors==0,"No unexpected runtime errors");Log("PASS three ratios + 240 actual Unity animation frames");stage=0;EditorApplication.isPlaying=false;
                EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);
            }
        }catch(Exception e){Log("FAIL "+e);stage=0;EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}
    }
    static void Resolution(int w,int h){
        var method=typeof(ShelterHomeValidation).GetMethod("Resolution",BindingFlags.Static|BindingFlags.NonPublic);method.Invoke(null,new object[]{w,h});
    }
}

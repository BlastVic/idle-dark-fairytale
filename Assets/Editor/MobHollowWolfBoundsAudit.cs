using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Spine;
using Spine.Unity;
public static class MobHollowWolfBoundsAudit {
 public static void Run(){
  var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(MobHollowWolfBuilder.Root+"/MobHollowWolf_SkeletonData.asset");var s=new Skeleton(data.GetSkeletonData(false));s.SetSkin("1");var report=new StringBuilder();
  foreach(var name in new[]{"Idle1","Attack1","Hit","Death1"}){
   var a=s.Data.FindAnimation(name);var bounds=new Bounds();bool first=true;float biggest=0;string culprit="";
   for(int f=0;f<=60;f++){
    s.SetToSetupPose();a.Apply(s,0,a.Duration*f/60f,false,null,1,MixBlend.Replace,MixDirection.In);s.UpdateWorldTransform();
    foreach(var slot in s.DrawOrder){if(slot.A<=0||!slot.Bone.Active||slot.Data.Name.ToLowerInvariant().Contains("shadow"))continue;float[] v;
     if(slot.Attachment is MeshAttachment m){v=new float[m.WorldVerticesLength];m.ComputeWorldVertices(slot,0,v.Length,v,0,2);}
     else if(slot.Attachment is RegionAttachment r){v=new float[8];r.ComputeWorldVertices(slot.Bone,v,0,2);}else continue;
     for(int j=0;j<v.Length;j+=2){var p=new Vector3(v[j],v[j+1],0);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);if(p.magnitude>biggest){biggest=p.magnitude;culprit=slot.Data.Name+" at "+a.Duration*f/60f+" "+p;}}
    }
   }report.AppendLine(name+" "+bounds+" extreme "+culprit);
  }File.WriteAllText(MobHollowWolfBuilder.Output+"/bounds.txt",report.ToString());
 }
}

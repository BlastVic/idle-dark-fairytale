using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scripts.Skills;
[InitializeOnLoad] public static class WolfPetAcceptance {
 const string Dir="output/wolf-pets/";static int phase,index,ratio,hits;static double next,start,diagnostic;static bool active;static string mode;static WolfPetCompanion pet;static float hp;static Actor_Base target;
 static readonly string[] PetIds={"PetShadowWolf","PetGhostWolf"};static readonly string[] SizeIds={"PetShadowWolf","PetGhostWolf","MobHexmaw","MobRivenfang"};static string[] Ids=>mode!=null&&mode.StartsWith("sizes-")?SizeIds:PetIds;static readonly int[] W={720,768,720},H={1280,1024,1440};
 static WolfPetAcceptance(){EditorApplication.update+=Tick;}
 static void Log(string s){File.AppendAllText(Dir+mode+".txt",s+"\n");}
 static void Check(bool ok,string s){if(!ok)throw new Exception(s);Log("PASS "+s);}
 static void Resolution(){typeof(MonsterPreviewValidation).GetMethod("SetResolution",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{W[ratio],H[ratio]});}
 public static void Begin(string m){if(EditorApplication.isPlaying)throw new Exception("Stop first");SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);SessionState.SetString("WOLF_QA",m);EditorSceneManager.OpenScene((m=="preview"||m.StartsWith("sizes-"))?MonsterTestSceneBuilder.ScenePath:"Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;}
 static void Finish(string result){ratio=0;Resolution();Log(result);active=false;SessionState.SetBool("WOLF_RETURN",true);EditorApplication.isPlaying=false;}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("WOLF_RETURN",false)){SessionState.SetBool("WOLF_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);return;}
  if(EditorApplication.isPlaying&&!active&&SessionState.GetString("WOLF_QA","")!=""){mode=SessionState.GetString("WOLF_QA","");SessionState.SetString("WOLF_QA","");active=true;phase=index=ratio=hits=0;start=EditorApplication.timeSinceStartup;next=start+3;File.WriteAllText(Dir+mode+".txt","Isolated wolf pet QA; save disabled\n");}
  if(!active||!EditorApplication.isPlaying)return;
  try{
   if(EditorApplication.timeSinceStartup>diagnostic){diagnostic=EditorApplication.timeSinceStartup+5;Log("phase="+phase+" player="+(bool)Player.single+" wave="+(bool)WaveManager.single+" scale="+Time.timeScale+" enemies="+(GameManager.single?GameManager.single.GetLivingEnemies().Count:-1));}
   if(EditorApplication.timeSinceStartup-start>150)throw new Exception("Timeout phase "+phase);
   if(mode=="combat"){
    if(!Player.single||!WaveManager.single||Player.single.ab.currentStat==null)return;
    var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();if(phase==0 && intro && intro.gameObject.activeInHierarchy && !intro.Finished)return;
    Player.single.StopAllCoroutines();Player.single.skeletonAnimation.timeScale=0;if(phase<12)Player.single.ab.currentStat.hpNow=100000;
    foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(e.ab&&e.ab.currentStat!=null&&!e.ab.isDying){e.StopAttacking();e.ab.currentStat.def=0;e.ab.currentStat.hpMax=100000;if(e.ab.currentStat.hpNow<1000)e.ab.currentStat.hpNow=100000;}
   }
   if(EditorApplication.timeSinceStartup<next)return;
   if(mode=="preview"||mode.StartsWith("sizes-")){Preview();return;}
   if(phase==0){
    SkillsController.Instance.cindrelLevel=0;SkillsController.Instance.shadowWolfLevel=0;
    var wm=WaveManager.single;var level=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));level.levelKey="WolfPetQA";level.levelName="Wolf Pet QA";level.mapGraphicKey="CH1_Node05";level.waves=new[]{new Wave{enemies=new[]{"MobTallowkin"}}};wm.waveDb.RemoveAll(w=>w.levelKey==level.levelKey);wm.waveDb.Add(level);
    var ui=GameplayCanvas.single;LevelController.Instance.AssetManager.HideMapPackage();if(ui.homeUI)ui.homeUI.gameObject.SetActive(false);ui.ToggleButtonGrid(false);ui.ToggleFusionBarUI(false);ui.victoryPanel.SetActive(false);ui.defeatPanel.SetActive(false);ui.dailyCherriesPop.SetActive(false);wm.CallStartLevel(level.levelKey);phase=1;next=EditorApplication.timeSinceStartup+3;return;
   }
   if(phase==1){if(GameManager.single.GetLivingEnemies(false).Count==0){Log("waiting enemies ready: "+GameManager.single.GetLivingEnemies().Count);return;}target=GameManager.single.GetLivingEnemies(false)[0];target.currentStat.hpNow=100000;hp=target.currentStat.hpNow;SkillsController.Instance.shadowWolfLevel=Ids[index]=="PetShadowWolf"?1:0;LevelController.Instance.AssetManager.SpawnGeneralObj(Ids[index]);phase=2;next=EditorApplication.timeSinceStartup+.1;return;}
   if(phase==2){pet=UnityEngine.Object.FindFirstObjectByType<WolfPetCompanion>();if(!pet)return;Check(pet.name==Ids[index]+"(Clone)",Ids[index]+" loaded through general DB");hits=0;pet.skeleton.AnimationState.Event+=(entry,e)=>{if(e.Data.Name=="OnHit")hits++;};phase=3;next=EditorApplication.timeSinceStartup+2.3;return;}
   if(phase==3){Check(target.currentStat.hpNow<hp,Ids[index]+" real target HP decreased");Check(hits>0,Ids[index]+" hit events observed");pet.StopAllCoroutines();pet.skeleton.AnimationState.SetAnimation(0,"Idle1",true);ratio=0;Resolution();phase=4;next=EditorApplication.timeSinceStartup+1;return;}
   if(phase==4){var assets=LevelController.Instance.AssetManager;var cam=assets.battleCampCamera.GetComponent<Camera>();assets.battleStyle.PlacePlayer(Player.single,cam);int slot=0;foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(!enemy.ab.isDying)assets.battleStyle.PlaceEnemy(enemy,cam,slot++);pet.Place();phase=15;next=EditorApplication.timeSinceStartup+3;return;}
   if(phase==15){ScreenCapture.CaptureScreenshot(Dir+Ids[index]+"-combat-"+W[ratio]+"x"+H[ratio]+".png");phase=5;next=EditorApplication.timeSinceStartup+.3;return;}
   if(phase==5){if(++ratio<3){Resolution();phase=4;next=EditorApplication.timeSinceStartup+1;return;}pet.Exit();phase=6;next=EditorApplication.timeSinceStartup+1.5;return;}
   if(phase==6){Check(!pet,Ids[index]+" exit destroys pet");if(++index<Ids.Length){phase=1;return;}LevelController.Instance.AssetManager.SpawnGeneralObj(Ids[1]);phase=7;next=EditorApplication.timeSinceStartup+1;return;}
   if(phase==7){pet=UnityEngine.Object.FindFirstObjectByType<WolfPetCompanion>();if(!pet)return;target.Hit(1000000);phase=8;next=EditorApplication.timeSinceStartup+4;return;}
   if(phase==8){if(!WaveManager.single.victoryCalled)return;Check(true,"live pet battle reaches victory");GameplayCanvas.single.GoToCampFromVictoryOrDefeat();phase=9;next=EditorApplication.timeSinceStartup+3;return;}
   if(phase==9){Check(!UnityEngine.Object.FindFirstObjectByType<WolfPetCompanion>(),"victory return cleans pet");WaveManager.single.CallStartLevel("WolfPetQA");phase=10;next=EditorApplication.timeSinceStartup+3;return;}
   if(phase==10){if(GameManager.single.GetLivingEnemies(false).Count==0)return;SkillsController.Instance.shadowWolfLevel=1;LevelController.Instance.AssetManager.SpawnGeneralObj(Ids[0]);phase=11;next=EditorApplication.timeSinceStartup+1;return;}
   if(phase==11){pet=UnityEngine.Object.FindFirstObjectByType<WolfPetCompanion>();if(!pet)return;Player.single.skeletonAnimation.timeScale=1;Player.single.ab.currentStat.block=0;Player.single.ab.currentStat.hpNow=1;Player.single.ab.Hit(1000000);phase=12;next=EditorApplication.timeSinceStartup+.2;return;}
   if(phase==12){Check(pet&&pet.Exiting,"player death cancels attack and starts exit");phase=13;next=EditorApplication.timeSinceStartup+1.5;return;}
   if(phase==13){Check(!pet,"defeat removes pet");Finish("PASS both pets: DB spawn, damage, exit, 3 ratios, victory cleanup and defeat");}
  }catch(Exception e){Finish("FAIL "+e);Debug.LogException(e);}
 }
 static void Preview(){
  var t=UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();if(!t)return;
  if(phase==0){int i=Array.FindIndex(t.monsters,d=>d.name==Ids[index]+"_SkeletonData");Check(i>=0,Ids[index]+" catalog");t.SelectMonster(i);t.stage.InitializePreview();t.stage.Apply();var scale=t.character.transform.lossyScale;foreach(var name in t.AnimationNames){t.Play(Array.IndexOf(t.AnimationNames,name));t.character.Update(.3f);t.character.LateUpdate();t.stage.Apply();Check(Vector3.Distance(scale,t.character.transform.lossyScale)<.0001f,"fixed scale "+name);}Log("SIZE "+Ids[index]+" scale="+scale.ToString("F6")+" multiplier="+t.CombatSizeMultiplier+" pet="+t.CompanionDisplaySize);t.Play(Array.IndexOf(t.AnimationNames,"Idle1"));Resolution();phase=1;next=EditorApplication.timeSinceStartup+1;return;}
  if(phase==1){ScreenCapture.CaptureScreenshot(Dir+mode+"-"+Ids[index]+"-preview-"+W[ratio]+"x"+H[ratio]+".png");phase=2;next=EditorApplication.timeSinceStartup+.3;return;}
  if(phase==2){if(++ratio<3){Resolution();phase=1;next=EditorApplication.timeSinceStartup+1;return;}ratio=0;if(++index<Ids.Length){phase=0;return;}Finish("PASS all selected clips; fixed authored size; 3 ratios");}
 }
}

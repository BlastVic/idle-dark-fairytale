using System;
using Assets.Scripts.Game.Enum;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scripts.Skills;

[InitializeOnLoad] public static class BookSpineDragonAcceptance {
 const string Dir="output/monster-animation/MobBookSpineDragon/";
 const string Key="BookSpine isolated validation";
 static bool active;static int phase,ratio,errors,hitCount,completeCount;static double next,started;
 static int fxLaunchBefore,fxHitBefore,fxFrame,observedBarrageCount;static bool fxShotLaunch,fxShotHit;
 static string mode,clip;static PetDragon pet;static Enemy target;static float before;static Vector3 fixedScale;
 static readonly int[] W={720,768,720},H={1280,1024,1440};
 static BookSpineDragonAcceptance(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(active&&(t==LogType.Error||t==LogType.Exception)){errors++;Log("ERROR "+m);}};}
 static void Log(string s){File.AppendAllText(Dir+mode+"-validation.txt",s+"\n");}
 static void Check(bool b,string s){if(!b)throw new Exception(s);Log("PASS "+s);}
 static void Resolution(int n){typeof(MonsterPreviewValidation).GetMethod("SetResolution",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{W[n],H[n]});}
 public static void Begin(string requested){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
  SessionState.SetString("BOOK_QA_MODE",requested);SessionState.SetBool("BOOK_QA_START",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);
  EditorSceneManager.OpenScene(requested=="preview"?MonsterTestSceneBuilder.ScenePath:"Assets/Scenes/Gameplay.unity");EditorApplication.isPlaying=true;
 }
 static void StartLevel(bool learned){
  SkillsController.Instance.taironTheDragonLevel=learned?1:0;SkillsController.Instance.breathOfHellLevel=0;
  var wm=WaveManager.single;var test=JsonUtility.FromJson<Waveset>(JsonUtility.ToJson(wm.waveDb[0]));test.levelKey=Key;test.levelName=Key;test.mapGraphicKey=(mode=="layout"||mode=="effects")?"CH1_Node05":"CH1_Node01";test.waves=new[]{new Wave{enemies=mode=="layout"?new[]{"MobWaxcap","MobWaxcap","MobRuneWolf"}:new[]{"MobWaxcap"}},new Wave{enemies=new[]{"MobBriarChild"}}};wm.waveDb.RemoveAll(w=>w.levelKey==Key);wm.waveDb.Add(test);
  var ui=GameplayCanvas.single;LevelController.Instance.AssetManager.HideMapPackage();if(ui.homeUI)ui.homeUI.gameObject.SetActive(false);ui.ToggleButtonGrid(false);ui.ToggleFusionBarUI(false);ui.victoryPanel.SetActive(false);ui.defeatPanel.SetActive(false);ui.dailyCherriesPop.SetActive(false);
  wm.CallStartLevel(Key);
 }
 static void Finish(string result){Log(result);active=false;SessionState.SetBool("BOOK_QA_RETURN",true);EditorApplication.isPlaying=false;}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool("BOOK_QA_RETURN",false)){SessionState.SetBool("BOOK_QA_RETURN",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);EditorSceneManager.OpenScene(MonsterTestSceneBuilder.ScenePath);return;}
  if(SessionState.GetBool("BOOK_QA_START",false)&&EditorApplication.isPlaying){SessionState.SetBool("BOOK_QA_START",false);mode=SessionState.GetString("BOOK_QA_MODE","");active=true;phase=0;errors=0;ratio=0;started=EditorApplication.timeSinceStartup;next=started+3;File.WriteAllText(Dir+mode+"-validation.txt","Book Spine "+mode+"; isolated runtime, no save writes\n");}
  if(!active||!EditorApplication.isPlaying )return;
  try{
   if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Timeout phase="+phase);
   if(mode=="preview"){if(EditorApplication.timeSinceStartup>=next)Preview();return;}
   var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>();if(intro&&!intro.Finished)return;
   if(!WaveManager.single||!Player.single||Player.single.ab.currentStat==null)return;
   if(phase<40){Player.single.ab.currentStat.hpNow=100000;Player.single.ab.currentStat.hpMax=100000;Player.single.StopAllCoroutines();Player.single.skeletonAnimation.timeScale=0;}
   foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)){if(!e.ab||e.ab.currentStat==null)continue;if(!e.ab.isDying){e.ab.currentStat.hpMax=100000;if(phase!=4)e.ab.currentStat.hpNow=100000;e.ab.currentStat.def=0;e.ab.chanceInterruptAttack=0;e.StopAttacking();}}
   if(mode=="effects"&&phase==4&&pet){float t=pet.skeletonAnimation.AnimationState.GetCurrent(0).TrackTime;float hitAt=BookSpinePetEffects.HitTime(pet.skeletonAnimation.Skeleton.Data.FindAnimation(clip));
    if(clip=="Attack3"){
     var shots=pet.GetComponent<BookSpinePetEffects>();
     if(shots.BarrageEmittedCount>observedBarrageCount){observedBarrageCount=shots.BarrageEmittedCount;Log("Attack3 emitted="+observedBarrageCount+" animationTime="+t.ToString("F3"));}
    }
    if(t>=fxFrame*.065f){ScreenCapture.CaptureScreenshot("output/pet-barrage-v2/"+clip+"-frame-"+fxFrame.ToString("D3")+".png");fxFrame++;}
    if(!fxShotLaunch&&t>=hitAt-.07f){ScreenCapture.CaptureScreenshot("output/pet-barrage-v2/"+clip+"-release.png");fxShotLaunch=true;}
    if(!fxShotHit&&t>=hitAt+.09f){ScreenCapture.CaptureScreenshot("output/pet-barrage-v2/"+clip+"-hit.png");fxShotHit=true;}
   }
   if(EditorApplication.timeSinceStartup<next)return;
   if(phase==0){StartLevel(false);phase=1;next=EditorApplication.timeSinceStartup+4;return;}
   if(phase==1){if(GameManager.single.GetLivingEnemies(false).Count==0)return;Check(!PetDragon.single,"unlearned skill spawns no companion");GameManager.single.SetGameState(GameStateType.CAMP);WaveManager.single.StopAllCoroutines();GameManager.single.BattleCleanup();phase=5;next=EditorApplication.timeSinceStartup+.5;return;}
   if(phase==5){StartLevel(true);phase=2;next=EditorApplication.timeSinceStartup+4;return;}
   if(phase==2){
    pet=PetDragon.single;if(!pet||GameManager.single.GetLivingEnemies(false).Count==0)return;
    target=GameManager.single.GetLivingEnemies(false)[0].GetComponent<Enemy>();
    Check(pet.name=="MobBookSpineDragon(Clone)"&&pet.GetComponent<BookSpineCompanion>(),"actual PetDragon general DB spawn uses new independent prefab");
    Check(UnityEngine.Object.FindObjectsByType<PetDragon>(FindObjectsSortMode.None).Length==1,"exactly one companion");
    Check(Mathf.Approximately(pet.myDmg,Mathf.Max(1,Player.single.ab.currentStat.dmg*SkillsController.Instance.GetSkillValues(1,SkillsTypeEnum.Tairon_The_Dragon)[0])),"original skill damage coefficient");
    pet.StopAllCoroutines();pet.currentAnimation="";pet.SetAnimation("Idle1",true,true);
    pet.skeletonAnimation.AnimationState.Event+=(entry,e)=>{if(!active)return;if(e.Data.Name=="OnHit" && phase==4){hitCount++;Check(target.ab.currentStat.hpNow<before,"actual target HP decreases from "+entry.Animation.Name);Check(IdleKnightHero.UI.SoundManager.Instance.audioSource.isPlaying,"attack audio playing");}if(e.Data.Name=="OnComplete"&&phase==4&&entry.Animation.Name.StartsWith("Attack"))completeCount++;};
    ratio=0;if(mode=="layout"){Resolution(0);phase=10;next=EditorApplication.timeSinceStartup+1;return;}if(mode=="effects"){
       Resolution(0);LevelController.Instance.AssetManager.SpawnParticle("Hit_Particle",target.rootPos.transform.position);
       phase=60;next=EditorApplication.timeSinceStartup+.12;return;
    }phase=3;return;
   }
   if(phase==60){ScreenCapture.CaptureScreenshot("output/pet-barrage-v2/reference-enemy-hit.png");phase=61;next=EditorApplication.timeSinceStartup+1;return;}
   if(phase==61){phase=3;return;}
   if(phase==3){
    pet.StopAllCoroutines();pet.breathOfHellChance=0;if(mode=="effects"){var fx=pet.GetComponent<BookSpinePetEffects>();Check(fx,"new companion has effects controller");fxLaunchBefore=fx.LaunchedCount;fxHitBefore=fx.ImpactCount;fxShotLaunch=fxShotHit=false;fxFrame=0;observedBarrageCount=0;}pet.currentAnimation="";clip="Attack"+(ratio+1);hitCount=completeCount=0;before=target.ab.currentStat.hpNow;pet.SetAnimation(clip,false,false);phase=4;next=EditorApplication.timeSinceStartup+pet.skeletonAnimation.Skeleton.Data.FindAnimation(clip).Duration+.04;return;
   }
   if(phase==4){if(completeCount==0 && pet.currentAnimation.StartsWith("Attack"))return;if(mode=="effects"){var fx=pet.GetComponent<BookSpinePetEffects>();if(clip=="Attack3")Check(observedBarrageCount==5,"Attack3 emits all five staggered shots");Check(fx.LaunchedCount==fxLaunchBefore+1,clip+" launches once");Check(fx.ImpactCount==fxHitBefore+1,clip+" impact once without extra damage callbacks");}Check(hitCount==1&&completeCount==1,clip+" exactly one live hit/completion");pet.StopAllCoroutines();Check(pet.currentAnimation=="Idle1","completion returns Idle1");if(++ratio<3){phase=3;return;}ratio=0;Resolution(0);phase=10;next=EditorApplication.timeSinceStartup+1;return;}
   if(phase==10){if(mode=="layout"||mode=="effects"){var assets=LevelController.Instance.AssetManager;var camera=assets.battleCampCamera.GetComponent<Camera>();assets.battleStyle.PlacePlayer(Player.single,camera);int index=0;foreach(var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).OrderByDescending(e=>e.skeletonAnimation.GetComponent<MeshRenderer>().sortingOrder))if(!enemy.ab.isDying)assets.battleStyle.PlaceEnemy(enemy,camera,index++);}pet.StopAllCoroutines();pet.currentAnimation="";pet.SetAnimation("Idle1",true,true);pet.GetComponent<BookSpineCompanion>().Place();if(mode=="effects"){pet.currentAnimation="";pet.SetAnimation("Attack3",false,false);}phase=11;next=EditorApplication.timeSinceStartup+(mode=="effects"?1.7:4);return;}
   if(phase==11){if(mode=="layout"){
    var enemies=UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e=>!e.ab.isDying).OrderBy(e=>e.skeletonAnimation.GetComponent<MeshRenderer>().sortingOrder).ToArray();
    for(int i=0;i<enemies.Length;i++){var e=enemies[i];e.StopAttacking();e.skeletonAnimation.AnimationState.SetAnimation(0,"Idle1",true);var body=e.skeletonAnimation.GetComponent<MeshRenderer>();var bar=e.ab.enemyLifebar.GetComponent<Canvas>();Check(bar.sortingLayerID==body.sortingLayerID&&bar.sortingOrder==body.sortingOrder+1,"bar follows owner depth "+e.name+" bar="+bar.sortingLayerName+":"+bar.sortingOrder+" body="+body.sortingLayerName+":"+body.sortingOrder);if(i+1<enemies.Length&&body.sortingOrder<enemies[i+1].skeletonAnimation.GetComponent<MeshRenderer>().sortingOrder)Check(bar.sortingOrder<enemies[i+1].skeletonAnimation.GetComponent<MeshRenderer>().sortingOrder,"rear bar below nearer body");}
    Check(pet.skeletonAnimation.transform.localScale==Vector3.one,"pet display node remains unit scale");
   }ScreenCapture.CaptureScreenshot(Dir+(mode=="layout"?"layout-":mode=="effects"?"effects-":"combat-")+W[ratio]+"x"+H[ratio]+".png");phase=12;next=EditorApplication.timeSinceStartup+.3;return;}
   if(phase==12){if(++ratio<3){Resolution(ratio);phase=10;next=EditorApplication.timeSinceStartup+1;return;}if(mode=="layout"){Resolution(0);Finish(errors==0?"PASS layout three aspect ratios; errors=0":"FAIL errors="+errors);return;}phase=20;return;}
   if(phase==20){pet.StopAllCoroutines();pet.breathOfHellChance=1;pet.currentAnimation="";pet.AnimationComplete(0,"Attack1");phase=21;next=EditorApplication.timeSinceStartup+.4;return;}
   if(phase==21){Check(pet.currentAnimation=="Attack2"||pet.currentAnimation=="Attack3","100% Breath Of Hell selects native special attack");pet.StopAllCoroutines();pet.currentAnimation="";pet.SetAnimation("Idle1",true,true);hitCount=0;target.ab.Hit(1000000);phase=22;next=EditorApplication.timeSinceStartup+4;return;}
   if(phase==22){if(WaveManager.single.waveNumber<1||GameManager.single.GetLivingEnemies(false).Count==0)return;Check(pet&&UnityEngine.Object.FindObjectsByType<PetDragon>(FindObjectsSortMode.None).Length==1,"wave transition preserves one companion");pet.StopAllCoroutines();pet.currentAnimation="";pet.SetAnimation("Idle1",true,true);foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(!e.ab.isDying)e.ab.Hit(1000000);phase=23;next=EditorApplication.timeSinceStartup+4;return;}
   if(phase==23){if(!WaveManager.single.victoryCalled)return;Check(true,"two isolated waves reach victory");GameplayCanvas.single.GoToCampFromVictoryOrDefeat();phase=24;next=EditorApplication.timeSinceStartup+4;return;}
   if(phase==24){if(mode=="effects")Check(UnityEngine.Object.FindObjectsByType<BookPetSpineEffect>(FindObjectsSortMode.None).Length==0,"victory leaves no effects");Check(!PetDragon.single,"victory return cleans up battle companion");StartLevel(true);phase=40;next=EditorApplication.timeSinceStartup+4;return;}
   if(phase==40){pet=PetDragon.single;if(!pet||GameManager.single.GetLivingEnemies(false).Count==0)return;pet.StopAllCoroutines();pet.currentAnimation="";pet.SetAnimation("Attack1",false,false);if(mode=="effects"){phase=43;next=EditorApplication.timeSinceStartup+.70;return;}Player.single.skeletonAnimation.timeScale=1;Player.single.ab.currentStat.block=0;Player.single.ab.currentStat.hpNow=1;Player.single.ab.Hit(1000000);phase=41;next=EditorApplication.timeSinceStartup+.2;return;}
   if(phase==43){Check(pet.GetComponent<BookSpinePetEffects>().ActiveCount>0,"projectile exists before death interruption");Player.single.skeletonAnimation.timeScale=1;Player.single.ab.currentStat.block=0;Player.single.ab.currentStat.hpNow=1;Player.single.ab.Hit(1000000);phase=41;next=EditorApplication.timeSinceStartup+.2;return;}
   if(phase==41){Check(pet&&pet.GetComponent<BookSpineCompanion>().Exiting,"actual player defeat starts ground Exit during attack");phase=42;next=EditorApplication.timeSinceStartup+2;return;}
   if(phase==42){if(mode=="effects")Check(UnityEngine.Object.FindObjectsByType<BookPetSpineEffect>(FindObjectsSortMode.None).Length==0,"death interruption leaves no effects");Check(!PetDragon.single,"ground Exit destroys companion");Finish(errors==0?"PASS live skill spawn / damage / specials / waves / victory / defeat / cleanup; errors=0":"FAIL errors="+errors);}
  }catch(Exception e){Finish("FAIL "+e);Debug.LogException(e);}
 }
 static void Preview(){
  var t=UnityEngine.Object.FindFirstObjectByType<MonsterAnimationTest>();if(!t)return;
  if(phase==0){int ix=Array.FindIndex(t.monsters,d=>d.name=="MobBookSpineDragon_SkeletonData");Check(ix>=0,"catalog entry");t.SelectMonster(ix);t.stage.InitializePreview();t.stage.Apply();fixedScale=t.character.transform.lossyScale;
   foreach(var name in t.AnimationNames){t.Play(Array.IndexOf(t.AnimationNames,name));t.character.Update(.2f);t.character.LateUpdate();t.stage.Apply();Check(Vector3.Distance(fixedScale,t.character.transform.lossyScale)<.0001f,"fixed scale "+name);}
   t.Play(Array.IndexOf(t.AnimationNames,"Idle1"));Resolution(0);phase=1;next=EditorApplication.timeSinceStartup+1;return;}
  if(phase==1){ScreenCapture.CaptureScreenshot(Dir+"preview-"+W[ratio]+"x"+H[ratio]+".png");phase=2;next=EditorApplication.timeSinceStartup+.3;return;}
  if(phase==2){t.Play(Array.IndexOf(t.AnimationNames,"Attack3"));phase=3;next=EditorApplication.timeSinceStartup+1.7;return;}
  if(phase==3){ScreenCapture.CaptureScreenshot(Dir+"preview-attack-"+W[ratio]+"x"+H[ratio]+".png");phase=6;next=EditorApplication.timeSinceStartup+.3;return;}
  if(phase==6){t.Play(Array.IndexOf(t.AnimationNames,"Idle1"));if(++ratio<3){Resolution(ratio);phase=1;next=EditorApplication.timeSinceStartup+1;return;}Resolution(0);var fx=t.character.GetComponent<BookSpinePetEffects>();if(fx){fxLaunchBefore=fx.LaunchedCount;fxHitBefore=fx.ImpactCount;t.SetLoop(true);t.Play(Array.IndexOf(t.AnimationNames,"Attack1"));phase=7;next=EditorApplication.timeSinceStartup+3.1;return;}Finish(errors==0?"PASS preview 24 clips / three aspect ratios; errors=0":"FAIL errors="+errors);}
  if(phase==7){var fx=t.character.GetComponent<BookSpinePetEffects>();Check(fx.LaunchedCount==fxLaunchBefore+2&&fx.ImpactCount==fxHitBefore+2,"two preview loops each launch and impact once: launches="+(fx.LaunchedCount-fxLaunchBefore)+" hits="+(fx.ImpactCount-fxHitBefore)+" track="+t.character.AnimationState.GetCurrent(0).TrackTime);t.SetSpeed(.25f);t.Play(Array.IndexOf(t.AnimationNames,"Attack1"));phase=8;next=EditorApplication.timeSinceStartup+3.6;return;}
  if(phase==8){var fx=t.character.GetComponent<BookSpinePetEffects>();float time=t.character.AnimationState.GetCurrent(0).TrackTime;Check(time>.5f&&time<1.1f&&fx.ActiveCount>0,"quarter-speed projectile survives until impact, track="+time);ScreenCapture.CaptureScreenshot("output/pet-barrage-v2/slow-preview.png");phase=9;next=EditorApplication.timeSinceStartup+.2;return;}
  if(phase==9){t.SetSpeed(1);t.Play(Array.IndexOf(t.AnimationNames,"Idle1"));Finish(errors==0?"PASS preview 24 clips / three aspect ratios / repeated effect loops / slow projectile; errors=0":"FAIL errors="+errors);}
 }
}

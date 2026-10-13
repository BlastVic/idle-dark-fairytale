using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using Assets.Scripts.Game.Enum;

// Captures the actual game with design-matching presentation data, without saving player data.
[InitializeOnLoad]
public static class ShelterFidelityValidation
{
    const string Dir="output/home-fidelity/";static int stage,ratio;static double due,deadline;
    static readonly int[] W={941,768,720},H={1672,1024,1440};static readonly string[] Names={"9x16","3x4","1x2"};
    static ShelterFidelityValidation(){if(SessionState.GetBool("SHELTER_FIDELITY_PENDING",false)){SessionState.SetBool("SHELTER_FIDELITY_PENDING",false);stage=1;due=EditorApplication.timeSinceStartup+3;deadline=due+150;}EditorApplication.update+=Tick;}
    static void Next(int n,float delay=1){stage=n;due=EditorApplication.timeSinceStartup+delay;}
    static void Resolution(int w,int h)=>typeof(ShelterHomeValidation).GetMethod("Resolution",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{w,h});
    static void Fixture(ShelterHomeUI ui){
        ui.enabled=false;ui.levelText.text="Lv. 17";ui.numberText.text="No. 017";ui.hpText.text="27 / 27";ui.xpText.text="6%";ui.hpFill.fillAmount=1;ui.xpFill.fillAmount=.06f;ui.crystalsText.text="1,280";ui.cherriesText.text="120";ui.pointsText.text="可用属性点  <color=#761E20><size=52>3</size></color>";
        string[] values={"27","12","5","100%","5%","150%"};for(int i=0;i<6;i++){ui.statValues[i].text=values[i];ui.statButtons[i].interactable=true;ui.statButtons[i].GetComponentInChildren<Text>().color=Color.black;}
        ui.skillBadge.transform.parent.gameObject.SetActive(true);ui.skillBadge.text="2";ui.rebirthLock.SetActive(true);
        string[] status={"可领取","18:24","1:42:08","空位"};for(int i=0;i<4;i++){
            ui.chestLabels[i].text=status[i];ui.chestLabels[i].color=i==3?Color.black:new Color(.98f,.94f,.84f);ui.chestButtons[i].interactable=i<3;ui.chestImages[i].enabled=i<3;ui.chestImages[i].color=Color.white;ui.chestImages[i].sprite=ui.chestSprites[Math.Min(i,2)];ui.chestReady[i].SetActive(i==0);ui.chestEmptyFrames[i].SetActive(i==3);
            var plate=ui.chestStatusPlates[i];plate.gameObject.SetActive(i<3);plate.sprite=i==0?ui.chestReadyPlate:ui.chestTimerPlate;plate.color=Color.white;var size=i==0?ui.chestReadyPlateSize:ui.chestTimerPlateSize;plate.rectTransform.sizeDelta=size;plate.rectTransform.anchoredPosition=new Vector2(-size.x*.5f,-(i==0?ui.chestReadyPlateTop:ui.chestTimerPlateTop));var interior=plate.transform.Find("PlainPlateInterior").GetComponent<Image>();interior.sprite=i==0?ui.chestReadyInterior:ui.chestTimerInterior;interior.material=i==0?ui.chestReadyFillMaterial:ui.chestTimerFillMaterial;interior.color=Color.white;
        }
        foreach(var sg in ui.GetComponentsInChildren<SkeletonGraphic>()){sg.freeze=true;sg.Skeleton.SetToSetupPose();sg.Skeleton.Data.FindAnimation(sg.startingAnimation).Apply(sg.Skeleton,0,0,false,null,1,Spine.MixBlend.Setup,Spine.MixDirection.In);sg.Skeleton.UpdateWorldTransform();sg.UpdateMesh();}
    }
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;try{
        if(stage==0){if(!File.Exists(Dir+"capture.request"))return;File.Delete(Dir+"capture.request");File.WriteAllText(Dir+"capture-log.txt","Design-matching fixture; actual Unity Play Mode\n");ratio=0;SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);deadline=EditorApplication.timeSinceStartup+150;Resolution(W[0],H[0]);if(!EditorApplication.isPlaying){SessionState.SetBool("SHELTER_FIDELITY_PENDING",true);EditorApplication.isPlaying=true;}Next(1,3);return;}
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Fidelity capture timed out");if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<due)return;
        var ui=GameplayCanvas.single?GameplayCanvas.single.homeUI:null;if(!ui||!GameManager.single||GameManager.single.gameState!=GameStateType.CAMP)return;
        if(stage==1){Fixture(ui);ui.GetComponent<ShelterResponsiveLayout>().Apply();Canvas.ForceUpdateCanvases();Next(2);return;}
        if(stage==2){var px=ui.GetComponent<Canvas>().pixelRect;File.AppendAllText(Dir+"capture-log.txt","Captured "+Names[ratio]+" "+px.width+"x"+px.height+"\n");ScreenCapture.CaptureScreenshot(Dir+"reference-match-"+Names[ratio]+".png");ShelterPointerProbe.Run(ui,Dir+"capture-log.txt");Next(3);return;}
        if(stage==3){ratio++;if(ratio<3){Resolution(W[ratio],H[ratio]);Next(1);return;}File.AppendAllText(Dir+"capture-log.txt","PASS capture complete\n");stage=0;EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}
    }catch(Exception e){File.AppendAllText(Dir+"capture-log.txt","FAIL "+e);stage=0;EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);}}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Game.Enum;
using IdleKnightHero.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StorybookSettingsValidation
{
    public const string Out="output/settings-ui/",TestKey="QA.Settings.SkipLoadingAnimation";
    static bool started;static double deadline;
    static StorybookSettingsValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("SETTINGS_QA",false)){int phase=SessionState.GetInt("SETTINGS_QA_PHASE",0);if(phase<2&&!SessionState.GetBool("SETTINGS_QA_FAILED",false)){SessionState.SetInt("SETTINGS_QA_PHASE",phase+1);SessionState.SetBool("SETTINGS_QA_PENDING",true);}else{SessionState.SetBool("SETTINGS_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);SessionState.EraseString("SETTINGS_QA_PREF_KEY");SessionState.EraseInt("SETTINGS_QA_SKIP");PlayerPrefs.DeleteKey(TestKey);PlayerPrefs.Save();Log(SessionState.GetBool("SETTINGS_QA_FAILED",false)?"FAIL COMPLETE":"PASS COMPLETE");}}};}
    static void Begin(){EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);started=false;deadline=EditorApplication.timeSinceStartup+180;StorybookEquipmentValidation.Resolution(941,1672);EditorApplication.isPlaying=true;}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","Settings and three actual startup runs. Isolated project; QA preference key only.\n");SessionState.SetBool("SETTINGS_QA",true);SessionState.SetBool("SETTINGS_QA_FAILED",false);SessionState.SetInt("SETTINGS_QA_PHASE",0);SessionState.SetString("SETTINGS_QA_PREF_KEY",TestKey);SessionState.EraseInt("SETTINGS_QA_SKIP");PlayerPrefs.DeleteKey(TestKey);PlayerPrefs.Save();Begin();}
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("SETTINGS_QA_PENDING",false)){SessionState.SetBool("SETTINGS_QA_PENDING",false);Begin();}
        if(EditorApplication.isPlaying&&SessionState.GetBool("SETTINGS_QA",false)&&!started&&GameManager.single&&GameplayCanvas.single){started=true;deadline=EditorApplication.timeSinceStartup+180;GameManager.single.StartCoroutine(new SettingsProbe().Run(SessionState.GetInt("SETTINGS_QA_PHASE",0)));}
        if(EditorApplication.isPlaying&&SessionState.GetBool("SETTINGS_QA",false)&&deadline>0&&EditorApplication.timeSinceStartup>deadline){SessionState.SetBool("SETTINGS_QA_FAILED",true);Log("FAIL watchdog");EditorApplication.isPlaying=false;}}
    public static void Log(string s)=>File.AppendAllText(Out+"validation.txt",s+"\n");
}
public sealed class SettingsProbe
{
    int failures,errors;StorybookSettingsUI ui;
    void Check(bool v,string n){StorybookSettingsValidation.Log((v?"PASS ":"FAIL ")+n);if(!v)failures++;}
    void Error(string m,string st,LogType type){if(type==LogType.Exception||type==LogType.Error){errors++;StorybookSettingsValidation.Log("ERROR "+m+"\n"+st);}}
    void Shot(string n){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(StorybookSettingsValidation.Out+n+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    void Hit(Button b,string label){var r=(RectTransform)b.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,label);}
    public IEnumerator Run(int phase){Application.logMessageReceived+=Error;var gm=GameManager.single;var gc=GameplayCanvas.single;var intro=UnityEngine.Object.FindFirstObjectByType<CursedBookIntro>(FindObjectsInactive.Include);
        if(!gm.HasSeenThis(CursedBookPrologue.SeenKey))gm.thingsSeenPermanent.Add(CursedBookPrologue.SeenKey);
        while(intro&&!intro.Finished)yield return null;yield return new WaitForSecondsRealtime(1);
        gc.CancelInvoke("TryShowingXpBoosterNotification");gc.xpBoosterNotification.SetActive(false);gc.dailyCherriesPop.SetActive(false);
        for(int i=0;i<20;i++)if(!gm.HasSeenThis("Tutorial-"+i))gm.thingsSeenPermanent.Add("Tutorial-"+i);
        if(TutorialManager.single){TutorialManager.single.automatic=false;if(TutorialManager.single.view)TutorialManager.single.view.Hide();}
        Check(intro&&intro.Finished&&intro.HomeReady,"startup "+phase+" waits for actual home readiness");
        if(phase==1){Check(intro.SkippedStartup&&intro.Elapsed==0&&intro.HomeTexture==null&&!intro.book.gameObject.activeSelf,"saved preference skips book motion and render-texture capture");Check(!intro.GetComponent<CursedBookIntroAudio>().enabled,"skip path disables page-turn audio");}
        else Check(!intro.SkippedStartup&&intro.Elapsed>0,"startup "+phase+" plays normal book animation");
        Check(gc.homeUI.gameObject.activeInHierarchy&&gc.homeUI.GetComponent<GraphicRaycaster>().enabled&&gm.gameState==GameStateType.CAMP,"startup "+phase+" interactive camp restored");yield return new WaitForEndOfFrame();Shot("startup-"+phase);
        var home=gc.homeUI;home.settingsButton.onClick.Invoke();ui=home.settingsUI;Check(ui&&ui.IsOpen&&!home.modal.activeSelf,"gear opens full-screen settings directly");
        yield return new WaitForSecondsRealtime(.8f);while(ShaderUtil.anythingCompiling)yield return null;
        if(phase==0){Check(!StorybookPreferences.SkipLoadingAnimation&&!PlayerPrefs.HasKey(StorybookSettingsValidation.TestKey),"new installation defaults to skip disabled");
            Check(ui.transform.lossyScale.x>0&&ui.group.alpha==1,"settings visible scale and opacity");int nodes=ui.GetComponentsInChildren<Transform>(true).Length;Check(ui.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"persistent prefab references resolve");
            int[] w={941,768,720},h={1672,1024,1440};string[] tags={"9x16","3x4","1x2"};
            for(int i=0;i<3;i++){StorybookEquipmentValidation.Resolution(w[i],h[i]);yield return new WaitForSecondsRealtime(.3f);var layout=ui.GetComponent<StorybookSettingsLayout>();layout.Apply();Canvas.ForceUpdateCanvases();ui.scroll.verticalNormalizedPosition=1;yield return new WaitForEndOfFrame();Shot(tags[i]+"-settings");Hit(ui.back,tags[i]+" back pointer");
                ui.scroll.verticalNormalizedPosition=0;yield return new WaitForEndOfFrame();Hit(ui.skip,tags[i]+" last option reachable");Shot(tags[i]+"-bottom");var corners=new Vector3[4];layout.page.GetWorldCorners(corners);Check(corners.All(c=>c.x>=-1&&c.y>=-1&&c.x<=Screen.width+1&&c.y<=Screen.height+1),tags[i]+" page fits viewport");
            }
            ui.music.value=.37f;ui.sound.value=.62f;Canvas.ForceUpdateCanvases();Check(Mathf.Abs(ui.music.fillRect.rect.width/ui.music.fillRect.parent.GetComponent<RectTransform>().rect.width-.37f)<.001f,"music fill matches its value");Check(Mathf.Abs(ui.sound.fillRect.rect.width/ui.sound.fillRect.parent.GetComponent<RectTransform>().rect.width-.62f)<.001f,"sound fill matches its value");Check(Mathf.Abs(SoundManager.Instance.musicLevel-.37f)<.001f&&ui.musicValue.text=="37%","music binding updates actual mixer setting");Check(Mathf.Abs(SoundManager.Instance.soundLevel-.62f)<.001f&&ui.soundValue.text=="62%","sound binding updates actual mixer setting");
            var buttonRect=(RectTransform)ui.skip.transform;var sealRect=ui.skipOff.rectTransform;var before=sealRect.position;var size=buttonRect.localScale;var evt=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ui.skip.OnPointerDown(evt);yield return new WaitForSecondsRealtime(.12f);Check(sealRect.position==before&&buttonRect.localScale==size,"toggle press keeps seal fixed");Check(ui.skipOff.canvasRenderer.GetColor().r<.9f,"toggle has tint press feedback");Shot("toggle-pressed");ui.skip.OnPointerUp(evt);yield return new WaitForSecondsRealtime(.12f);Check(sealRect.position==before&&buttonRect.localScale==size,"toggle release keeps seal fixed");
            var originalMaterial=ui.skipOff.material;ui.skip.interactable=false;yield return null;yield return new WaitForEndOfFrame();Check(ui.skipOff.material!=originalMaterial,"disabled toggle uses grey state");ui.skip.interactable=true;yield return null;yield return new WaitForEndOfFrame();Check(ui.skipOff.material==originalMaterial,"enabled toggle restores artwork");
            ui.skip.onClick.Invoke();Check(StorybookPreferences.SkipLoadingAnimation&&PlayerPrefs.GetInt(StorybookSettingsValidation.TestKey,0)==1&&ui.skipMark.enabled,"enable writes persistent preference and check state");
            Check(!ui.skipOff.enabled&&ui.skipMark.enabled&&ui.skipOff.rectTransform.anchoredPosition==ui.skipMark.rectTransform.anchoredPosition&&ui.skipOff.rectTransform.sizeDelta==ui.skipMark.rectTransform.sizeDelta,"wax states share exact bounds");var selectedPosition=ui.skipMark.rectTransform.position;ui.skip.OnPointerDown(evt);yield return new WaitForSecondsRealtime(.12f);Check(ui.skipMark.rectTransform.position==selectedPosition&&buttonRect.localScale==size,"selected seal stays fixed when pressed");ui.skip.OnPointerUp(evt);
            ui.back.onClick.Invoke();Check(!ui.IsOpen&&!ui.group.blocksRaycasts,"close hides page and releases raycast blocking");Hit(home.settingsButton,"home gear clickable after close");home.settingsButton.onClick.Invoke();Check(ui.skipValue.text=="已开启","reopen preserves setting");Check(ui.GetComponentsInChildren<Transform>(true).Length==nodes,"no runtime UI generation");
            yield return new WaitForSecondsRealtime(.2f);Shot("toggle-selected");var layoutSafe=ui.GetComponent<StorybookSettingsLayout>();layoutSafe.validationSafe=new Rect(0,60,Screen.width,Screen.height-110);layoutSafe.Apply();yield return new WaitForEndOfFrame();Shot("1x2-safe-area");Hit(ui.back,"safe area back pointer");
        }else if(phase==1){Check(StorybookPreferences.SkipLoadingAnimation&&ui.skipMark.enabled,"setting survives actual play-mode restart");ui.skip.onClick.Invoke();Check(!StorybookPreferences.SkipLoadingAnimation&&PlayerPrefs.GetInt(StorybookSettingsValidation.TestKey,1)==0,"turn off is persisted for next startup");}
        else Check(!StorybookPreferences.SkipLoadingAnimation&&!ui.skipMark.enabled,"disabled preference persists after restart");
        Check(errors==0,"phase "+phase+" no runtime errors");Application.logMessageReceived-=Error;if(failures>0||errors>0)SessionState.SetBool("SETTINGS_QA_FAILED",true);EditorApplication.isPlaying=false;
    }
}

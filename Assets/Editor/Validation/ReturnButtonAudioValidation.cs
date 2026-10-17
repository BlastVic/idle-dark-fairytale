using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ReturnButtonAudioValidation
{
    const string Out="output/return-audio/";
    static bool running;
    struct Entry { public string path;public long id;public Entry(string p,long i){path=p;id=i;} }
    static readonly Entry[] entries={
        new Entry("Assets/Prefab/Addressable Prefabs/Packages/FusionPackage.prefab", 4321545693146284458L),
        new Entry("Assets/Prefab/Addressable Prefabs/Packages/FusionPackage.prefab", 9184505917296274091L),
        new Entry("Assets/Prefab/Addressable Prefabs/Packages/RebirthPackage.prefab", 8199433541247188862L),
        new Entry("Assets/Prefab/Addressable Prefabs/Packages/CashMallPackage.prefab", 7041045860234082658L),
        new Entry("Assets/Prefab/Addressable Prefabs/Packages/ChestPackage.prefab", 5786239049469479278L),
        new Entry("Assets/DarkFairytale/UI/Experience/StorybookExperience.prefab", 4338733468772255392L),
        new Entry("Assets/DarkFairytale/UI/Settings/StorybookSettings.prefab", 7076790433520522850L),
        new Entry("Assets/DarkFairytale/UI/Equipment/Prefabs/StorybookEquipment.prefab", 9116653437516497506L),
        new Entry("Assets/DarkFairytale/UI/Skills/Prefabs/StorybookSkills.prefab", 4105394804998189911L),
        new Entry("Assets/DarkFairytale/UI/CursedStorybook/Prefabs/CursedStorybookBattleHUD.prefab", 6137573925967777757L),
        new Entry("Assets/DarkFairytale/UI/Map/Prefabs/Chapter01Map.prefab", 2004539085201436029L),
        new Entry("Assets/DarkFairytale/UI/DailyReward/Prefabs/StorybookDailyReminder.prefab", 4663309372886791066L)
    };
    static ReturnButtonAudioValidation(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||running)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Out+"validate.request")){
            File.Delete(Out+"validate.request");File.WriteAllText(Out+"validation.txt","");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            SessionState.SetBool("RETURN_AUDIO_QA",true);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",true);EditorApplication.isPlaying=true;
        }
        if(EditorApplication.isPlaying&&SessionState.GetBool("RETURN_AUDIO_QA",false)&&GameManager.single&&IdleKnightHero.UI.SoundManager.Instance){running=true;GameManager.single.StartCoroutine(Run());}
    }
    static void Check(bool ok,string message){File.AppendAllText(Out+"validation.txt",(ok?"PASS ":"FAIL ")+message+"\n");}
    static IEnumerator Run(){
        yield return new WaitForSecondsRealtime(2);
        foreach(var entry in entries){
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(entry.path);Button source=null;
            foreach(var b in prefab.GetComponentsInChildren<Button>(true))if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(b,out string guid,out long id)&&id==entry.id)source=b;
            Check(source&&source.GetComponent<StorybookReturnAudio>(),entry.path+" authored audio "+entry.id);
            if(!source)continue;
            var clone=Object.Instantiate(source.gameObject);clone.SetActive(true);var button=clone.GetComponent<Button>();button.interactable=true;
            var ev=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            yield return null;int before=StorybookNavigation.ClickSoundCount;
            ExecuteEvents.Execute(clone,ev,ExecuteEvents.pointerClickHandler);
            Check(StorybookNavigation.ClickSoundCount==before+1,"enabled exactly once "+entry.id);
            StorybookNavigation.PlayClick();
            Check(StorybookNavigation.ClickSoundCount==before+1,"navigation sound deduplicated "+entry.id);
            yield return null;button.interactable=false;before=StorybookNavigation.ClickSoundCount;
            ExecuteEvents.Execute(clone,ev,ExecuteEvents.pointerClickHandler);
            Check(StorybookNavigation.ClickSoundCount==before,"disabled silent "+entry.id);
            yield return null;button.interactable=true;before=StorybookNavigation.ClickSoundCount;
            ExecuteEvents.Execute(clone,ev,ExecuteEvents.pointerClickHandler);
            Check(StorybookNavigation.ClickSoundCount==before+1,"restored sound "+entry.id);
            Object.Destroy(clone);
        }
        File.AppendAllText(Out+"validation.txt","DONE\n");SessionState.SetBool("RETURN_AUDIO_QA",false);SessionState.SetBool("MONSTER_QA_DISABLE_SAVE",false);running=false;EditorApplication.isPlaying=false;
    }
}

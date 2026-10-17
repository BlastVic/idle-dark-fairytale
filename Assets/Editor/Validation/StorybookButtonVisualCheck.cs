using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class StorybookButtonVisualCheck
{
    const string Dir="output/button-state/";
    static StorybookButtonVisualCheck(){EditorApplication.update+=Tick;}
    static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Dir+"check.request"))return;File.Delete(Dir+"check.request");Run();}
    public static void Batch(){StorybookButtonStateRepair.Repair();Run();}
    public static void Run(){
        var scene=EditorSceneManager.NewPreviewScene();var cameraGo=new GameObject("QA Camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene);
        var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.55f,.53f,.49f);camera.transform.position=new Vector3(0,0,-10);camera.scene=scene;
        var canvasGo=new GameObject("QA Canvas",typeof(RectTransform),typeof(Canvas));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo,scene);var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var texture=new RenderTexture(1080,960,24);camera.targetTexture=texture;
        var report="Actual Unity renderer; columns: normal / disabled / restored\n";
        string[] paths={"Map/Chapter01Map.prefab","Equipment/StorybookEquipment.prefab","Skills/StorybookSkills.prefab","Shelter/ShelterHome.prefab","Shelter/ShelterHome.prefab","CursedStorybook/CursedStorybookBattleHUD.prefab"};
        try{
            for(int row=0;row<paths.Length;row++){
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DarkFairytale/UI/"+paths[row]);
                Button selected;
                if(row==0)selected=source.GetComponent<StorybookMapUI>().enterButton;
                else if(row==1)selected=source.GetComponent<StorybookEquipmentUI>().equip;
                else if(row==2)selected=source.GetComponent<StorybookSkillsUI>().purchase;
                else if(row==3)selected=source.GetComponent<ShelterHomeUI>().battleButton;
                else if(row==4)selected=source.GetComponent<ShelterHomeUI>().statButtons[0];
                else selected=source.GetComponentInChildren<Button>(true);
                for(int col=0;col<3;col++){
                    var go=Object.Instantiate(selected.gameObject,canvasGo.transform);go.SetActive(true);var rect=(RectTransform)go.transform;
                    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(-360+360*col,390-155*row);
                    rect.sizeDelta=((RectTransform)selected.transform).rect.size;float scale=Mathf.Min(310/Mathf.Max(1,rect.sizeDelta.x),100/Mathf.Max(1,rect.sizeDelta.y));rect.localScale=Vector3.one*scale;
                    var button=go.GetComponent<Button>();var state=go.GetComponent<StorybookEquipmentButtonState>();state.SendMessage("Awake");
                    var initialMaterials=go.GetComponentsInChildren<Graphic>(true).ToDictionary(g=>g,g=>g.material);
                    button.interactable=col==0;state.SendMessage("LateUpdate");if(col==2){button.interactable=true;state.SendMessage("LateUpdate");}
                    var graphics=go.GetComponentsInChildren<Graphic>(true).Where(g=>g.GetComponentInParent<Button>()==button);
                    bool valid=graphics.All(g=>col==1 ? g.material.HasProperty("_DisabledUI")&&g.material.GetFloat("_DisabledUI")>.99f : g.material==initialMaterials[g]);
                    report+=(valid?"PASS ":"FAIL ")+paths[row]+" "+selected.name+" state "+col+"\n";
                }
            }
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;var image=new Texture2D(1080,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1080,960),0,0);image.Apply();File.WriteAllBytes(Dir+"normal-disabled-restored.png",image.EncodeToPNG());Object.DestroyImmediate(image);File.WriteAllText(Dir+"validation.txt",report);
        }finally{RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);}
    }
}

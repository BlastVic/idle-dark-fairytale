using System;
using System.IO;
using System.Linq;
using System.Text;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit authoring only. Importing or playing never changes the saved prefab.</summary>
[InitializeOnLoad]
public static class ShelterIdleBuilder
{
    public const string Root="Assets/DarkFairytale/UI/Shelter/RedHoodIdle";
    public const string Output="output/home-idle/";
    static ShelterIdleBuilder(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!File.Exists(Output+"install.request"))return;
        File.Delete(Output+"install.request");
        try{Install();File.WriteAllText(Output+"install-result.txt","PASS");}
        catch(Exception e){File.WriteAllText(Output+"install-result.txt",e.ToString());Debug.LogException(e);}
    }
    static Texture2D Read(string file){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(Root+"/"+file));return t;}
    static Texture2D Crop(Texture2D input,RectInt rect)
    {
        var t=new Texture2D(rect.width,rect.height,TextureFormat.RGBA32,false);
        t.SetPixels(input.GetPixels(rect.x,input.height-rect.y-rect.height,rect.width,rect.height));t.Apply();return t;
    }
    static RectInt Bounds(Texture2D input)
    {
        var c=input.GetPixels32();int minX=input.width,minY=input.height,maxX=0,maxY=0;
        for(int y=0;y<input.height;y++)for(int x=0;x<input.width;x++)if(c[y*input.width+x].a>2){minX=Mathf.Min(x,minX);maxX=Mathf.Max(x,maxX);minY=Mathf.Min(y,minY);maxY=Mathf.Max(y,maxY);}
        return new RectInt(minX,input.height-1-maxY,maxX-minX+1,maxY-minY+1);
    }
    static Texture2D Import(string path)
    {
        if(!AssetDatabase.LoadAssetAtPath<Texture2D>(path))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType=TextureImporterType.Default;i.alphaSource=TextureImporterAlphaSource.FromInput;i.alphaIsTransparency=true;
        i.mipmapEnabled=false;i.sRGBTexture=true;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=4096;i.wrapMode=TextureWrapMode.Clamp;i.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static T Asset<T>(string path,Func<T> create) where T:UnityEngine.Object
    {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=create();AssetDatabase.CreateAsset(asset,path);}return asset;}
    [MenuItem("Tools/Dark Fairytale/UI/Install Seated Spine Idle")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit play mode before explicit authoring.");
        Directory.CreateDirectory(Output);
        var body=new Texture2D(2,2,TextureFormat.RGBA32,false);body.LoadImage(File.ReadAllBytes("Assets/DarkFairytale/UI/Shelter/Refined/RedHoodShortCape.png"));var blink=Read("RedHoodBlink.png");var steamSource=Read("TeaSteam.png");
        var eyes=Crop(blink,new RectInt(588,140,144,94));var steam=Crop(steamSource,Bounds(steamSource));
        var textures=new[]{body,eyes,steam};var names=new[]{"body","eyes","steam"};
        string source="ArtDirection/Characters/RedHoodShelter/SpineSource";
        Directory.CreateDirectory(source+"/images");
        for(int i=0;i<textures.Length;i++)File.WriteAllBytes(source+"/images/"+names[i]+".png",textures[i].EncodeToPNG());
        File.Copy(Root+"/RedHoodShelter.json",source+"/RedHoodShelter.json",true);
        var atlas=new Texture2D(2048,2048,TextureFormat.RGBA32,false);
        var rects=atlas.PackTextures(textures,4,2048,false);
        var png=atlas.EncodeToPNG();string pngPath=Root+"/RedHoodShelter.png";
        if(!File.Exists(pngPath)||!File.ReadAllBytes(pngPath).SequenceEqual(png)){File.WriteAllBytes(pngPath,png);AssetDatabase.ImportAsset(pngPath,ImportAssetOptions.ForceSynchronousImport);}
        var text=new StringBuilder("RedHoodShelter.png\nsize: "+atlas.width+","+atlas.height+"\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\n");
        for(int j=0;j<rects.Length;j++){
            var r=rects[j];int x=Mathf.RoundToInt(r.x*atlas.width),y=Mathf.RoundToInt((1-r.yMax)*atlas.height),w=Mathf.RoundToInt(r.width*atlas.width),h=Mathf.RoundToInt(r.height*atlas.height);
            if(w!=textures[j].width||h!=textures[j].height)throw new Exception("Atlas unexpectedly downscaled "+names[j]);
            text.Append(names[j]+"\n  rotate: false\n  xy: "+x+", "+y+"\n  size: "+w+", "+h+"\n  orig: "+w+", "+h+"\n  offset: 0, 0\n  index: -1\n");
        }
        var texture=Import(Root+"/RedHoodShelter.png");var bg=Import(Root+"/ShelterEmpty.png");
        var material=Asset(Root+"/RedHoodShelter_UI.mat",()=>new Material(Shader.Find("Spine/SkeletonGraphic")));
        material.mainTexture=texture;material.SetFloat("_StraightAlphaInput",1);material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        string atlasPath=Root+"/RedHoodShelter.atlas.txt";
        if(!File.Exists(atlasPath)||File.ReadAllText(atlasPath)!=text.ToString()){File.WriteAllText(atlasPath,text.ToString());AssetDatabase.ImportAsset(atlasPath,ImportAssetOptions.ForceSynchronousImport);}
        AssetDatabase.ImportAsset(Root+"/RedHoodShelter.json",ImportAssetOptions.ForceSynchronousImport);
        var aa=Asset(Root+"/RedHoodShelter_Atlas.asset",()=>ScriptableObject.CreateInstance<SpineAtlasAsset>());
        aa.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/RedHoodShelter.atlas.txt");aa.materials=new[]{material};aa.Clear();EditorUtility.SetDirty(aa);
        var data=Asset(Root+"/RedHoodShelter_SkeletonData.asset",()=>ScriptableObject.CreateInstance<SkeletonDataAsset>());
        // Spine 3.8 SkeletonGraphic multiplies mesh units by Canvas referencePixelsPerUnit (100).
        data.skeletonJSON=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/RedHoodShelter.json");data.atlasAssets=new AtlasAssetBase[]{aa};data.scale=.0046f*1080/1086;data.defaultMix=.15f;data.Clear();EditorUtility.SetDirty(data);
        var skeleton=data.GetSkeletonData(false);if(skeleton==null||skeleton.FindAnimation("Idle")==null)throw new Exception("Spine Idle did not load.");
        var background=Asset(Root+"/ShelterEmpty.asset",()=>Sprite.Create(bg,new Rect(0,0,bg.width,bg.height),new Vector2(.5f,.5f),100));
        string characterPath=Root+"/RedHoodShelter.prefab";
        if(!File.Exists(characterPath)){
            var actor=new GameObject("RedHoodShelter",typeof(RectTransform),typeof(CanvasRenderer),typeof(SkeletonGraphic));
            try{
                var rt=(RectTransform)actor.transform;rt.sizeDelta=new Vector2(600,620);
                var sg=actor.GetComponent<SkeletonGraphic>();sg.skeletonDataAsset=data;sg.material=material;sg.startingAnimation="Idle";sg.startingLoop=true;sg.unscaledTime=true;sg.raycastTarget=false;sg.Initialize(true);
                PrefabUtility.SaveAsPrefabAsset(actor,characterPath);
            }finally{UnityEngine.Object.DestroyImmediate(actor);}
        }
        var home=PrefabUtility.LoadPrefabContents(ShelterHomeBuilder.Path);
        try{
            var scene=home.transform.Find("Core_9x16/ShelterIllustration")??home.transform.Find("SupportedViewport/ShelterIllustration");if(!home.GetComponent<ShelterResponsiveLayout>())scene.GetComponent<Image>().sprite=background;
            // Add once; subsequent explicit atlas refreshes preserve manual placement.
            if(!scene.Find("RedHoodShelter")){
                var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(characterPath),scene);
                var rt=(RectTransform)actor.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(.5f,0);
                rt.anchoredPosition=new Vector2((140+627*.46f)*1080/1086,-(510+1254*.46f)*1080/1086);
            }
            PrefabUtility.SaveAsPrefabAsset(home,ShelterHomeBuilder.Path);
        }finally{PrefabUtility.UnloadPrefabContents(home);}
        AssetDatabase.SaveAssets();
        foreach(var t in new[]{body,blink,steamSource,eyes,steam,atlas})UnityEngine.Object.DestroyImmediate(t);
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
        Debug.Log("SHELTER_IDLE_INSTALLED: "+skeleton.Bones.Count+" bones, "+skeleton.FindAnimation("Idle").Duration+" seconds");
    }
}

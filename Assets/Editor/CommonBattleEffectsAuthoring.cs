using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicit authoring only. Import and play never rewrite prefabs.
[InitializeOnLoad]
public static class CommonBattleEffectsAuthoring
{
    public const string Root="Assets/DarkFairytale/Effects/Common";
    public const string Out="output/common-effects/";
    public static readonly string[] Ids={"Hit","Shield","AOE","Heal","Res","Lightning","Absorb"};
    public static readonly string[] Names={"受击","荆棘格挡","回旋镰斩","灰叶治疗","残翼复生","裂隙雷霆","蜡封护佑"};
    public static readonly string[] Files={"Hit Particles","Shield Particles","AOE Particles","Particle System Heal","Ressurrection Particles","Lightning Particles","My Lucky Stars Particle"};
    public static string PathAt(int i)=>"Assets/Prefab/Addressable Prefabs/Particles/"+Files[i]+".prefab";
    static CommonBattleEffectsAuthoring(){EditorApplication.update+=()=>{
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(File.Exists(Out+"refresh-hit.request")){
            File.Delete(Out+"refresh-hit.request");
            try{RefreshHitPreview();File.WriteAllText(Out+"refresh-hit.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"refresh-hit.txt",e.ToString());Debug.LogException(e);}
            return;
        }
        if(!File.Exists(Out+"build.request"))return;
        File.Delete(Out+"build.request");try{Build();File.WriteAllText(Out+"build.txt","PASS");}catch(Exception e){File.WriteAllText(Out+"build.txt",e.ToString());Debug.LogException(e);}
    };}
    [MenuItem("Tools/Dark Fairytale/Effects/Apply Cursed Common Textures (Explicit)")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
        Directory.CreateDirectory(Out);Directory.CreateDirectory(Root+"/Materials");
        AssetDatabase.Refresh();
        for(int i=1;i<Ids.Length;i++){ // Hit uses the user-selected Spine effect.
            if(i==2||i==3||i==5)continue; // User-selected Spine healing and lightning effects.
            string path=Root+"/Textures/"+Ids[i]+".png";
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);string mp=Root+"/Materials/"+Ids[i]+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);if(!mat){mat=new Material(Shader.Find("DarkFairytale/Cursed Particle"));AssetDatabase.CreateAsset(mat,mp);}mat.mainTexture=tex;EditorUtility.SetDirty(mat);
            var root=PrefabUtility.LoadPrefabContents(PathAt(i));
            try{
                var depth=root.GetComponent<CursedBattleParticleLayer>();if(!depth)depth=root.AddComponent<CursedBattleParticleLayer>();depth.alignToPlayer=i==1||i==3||i==4||i==6;
                var systems=root.GetComponentsInChildren<ParticleSystem>(true);
                root.transform.localScale=Vector3.one;
                for(int layer=0;layer<systems.Length;layer++){
                    var ps=systems[layer];ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    bool primary=ps.gameObject==root;
                    var renderer=ps.GetComponent<ParticleSystemRenderer>();
                    renderer.sharedMaterial=mat;renderer.sortingLayerName="Character";renderer.sortingOrder=70;renderer.sortingFudge=-.1f;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;renderer.pivot=i==5?new Vector3(0,.4f,0):i==1?new Vector3(0,-1,0):Vector3.zero;
                    var uv=ps.textureSheetAnimation;uv.enabled=false;
                    var main=ps.main;main.loop=false;main.duration=.5f;main.startDelay=0;main.startColor=Color.white;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.startSize3D=false;
                    main.startRotation=0;main.startRotation3D=false;main.startSpeed=0;main.gravityModifier=0;main.maxParticles=32;
                    var velocity=ps.velocityOverLifetime;velocity.enabled=false;var force=ps.forceOverLifetime;force.enabled=false;var noise=ps.noise;noise.enabled=false;
                    var inherit=ps.inheritVelocity;inherit.enabled=false;var limit=ps.limitVelocityOverLifetime;limit.enabled=false;
                    var rotation=ps.rotationOverLifetime;rotation.enabled=false;var sizeSpeed=ps.sizeBySpeed;sizeSpeed.enabled=false;var colorSpeed=ps.colorBySpeed;colorSpeed.enabled=false;
                    var trails=ps.trails;trails.enabled=false;var shape=ps.shape;shape.enabled=false;
                    ps.transform.localScale=Vector3.one;ps.transform.localRotation=Quaternion.identity;
                    if(!primary)ps.transform.localPosition=Vector3.zero;
                    float[] sizes={1.7f,1.65f,3.7f,.42f,2.8f,3.2f,.42f};
                    float[] lifetimes={.30f,.42f,.65f,1.1f,1.3f,.45f,1.1f};
                    main.startSize=sizes[i]*(primary?1:.86f);main.startLifetime=lifetimes[i];
                    int count=1;
                    bool motes=i==3||i==6||(i==4&&!primary);
                    if(motes){count=6;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.48f;main.startSpeed=.2f;main.startSize=i==4?.17f:sizes[i];velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;velocity.x=0;velocity.y=.65f;velocity.z=0;}
                    if(!primary&&!motes)main.startColor=new Color(1,1,1,.22f);
                    var emission=ps.emission;emission.enabled=true;emission.rateOverTime=0;emission.rateOverDistance=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
                    var col=ps.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.85f,.45f),new GradientAlphaKey(0,1)});col.color=gradient;
                    var size=ps.sizeOverLifetime;size.enabled=true;size.separateAxes=false;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.65f),new Keyframe(.2f,1),new Keyframe(1,1.12f)));
                }
                PrefabUtility.SaveAsPrefabAsset(root,PathAt(i));
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();CommonEffectsSizing.Apply();InstallCatalog();
    }
    // Explicit refresh updates only the restored hit entry, preserving the authored UI.
    public static void RefreshHitPreview()
    {
        var root=PrefabUtility.LoadPrefabContents(SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab");
        try{
            var t=root.GetComponent<SpineEffectsTest>();
            t.particleBounds[0]=Measure(t.particleEffects[0],out t.particleDurations[0]);
            t.displayNames[5]=Names[0];
            t.selectionButtons[5].GetComponentInChildren<Text>().text=Names[0];
            PrefabUtility.SaveAsPrefabAsset(root,SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    public static Bounds Measure(GameObject prefab,out float duration)
    {
        var spine=prefab.GetComponent<CommonSpineBattleEffect>();if(spine){duration=spine.duration;return spine.motionBounds;}
        var go=UnityEngine.Object.Instantiate(prefab);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;go.transform.localScale=Vector3.one;
        foreach(var t in go.GetComponentsInChildren<DisableDestroyOnTimer>(true)){t.CancelInvoke();t.enabled=false;}
        var systems=go.GetComponentsInChildren<ParticleSystem>(true);duration=0;
        for(int i=0;i<systems.Length;i++){var ps=systems[i];ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(123+i);duration=Mathf.Max(duration,ps.main.startDelay.constantMax+ps.main.duration+ps.main.startLifetime.constantMax);}
        duration=Mathf.Min(5,duration);bool any=false;Bounds b=new Bounds();
        var particles=new ParticleSystem.Particle[256];
        for(int frame=1;frame<=90;frame++)foreach(var ps in systems){
            ps.Simulate(duration*frame/90,false,true,true);int count=ps.GetParticles(particles);
            for(int j=0;j<count;j++){
                if(particles[j].GetCurrentColor(ps).a<8)continue;
                var pos=ps.main.simulationSpace==ParticleSystemSimulationSpace.World?particles[j].position:ps.transform.TransformPoint(particles[j].position);
                float size=particles[j].GetCurrentSize(ps)*Mathf.Abs(ps.transform.lossyScale.x);
                pos+=ps.GetComponent<ParticleSystemRenderer>().pivot*size;var next=new Bounds(pos,Vector3.one*size);if(!any){b=next;any=true;}else b.Encapsulate(next);
            }
        }
        UnityEngine.Object.DestroyImmediate(go);return any?b:new Bounds(Vector3.zero,Vector3.one);
    }
    public static void InstallCatalog()
    {
        var root=PrefabUtility.LoadPrefabContents(SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab");
        try{
            var t=root.GetComponent<SpineEffectsTest>();var environments=Resources.Load<ChapterEnvironmentCatalog>("Chapter01Environments");if(environments&&environments.entries.Length>0)t.backdrop.sprite=environments.entries[0].prefab.GetComponent<SpriteRenderer>().sprite;t.particleEffects=new GameObject[7];t.particleBounds=new Bounds[7];t.particleDurations=new float[7];
            var names=t.displayNames.Take(5).ToList();names.AddRange(Names);t.displayNames=names.ToArray();
            var buttons=t.selectionButtons.Take(5).ToList();var template=buttons[0];var catalog=(RectTransform)template.transform.parent;
            catalog.offsetMin=new Vector2(0,-300);
            var core=t.safeRoot.Find("Core") as RectTransform;
            if(!core){core=new GameObject("Core",typeof(RectTransform)).GetComponent<RectTransform>();core.SetParent(t.safeRoot,false);core.anchorMin=new Vector2(.5f,0);core.anchorMax=new Vector2(.5f,1);core.sizeDelta=new Vector2(720,0);core.anchoredPosition=Vector2.zero;
                var controls=t.safeRoot.Find("Controls") as RectTransform;catalog.SetParent(core,false);catalog.anchorMin=new Vector2(0,1);catalog.anchorMax=Vector2.one;catalog.offsetMin=new Vector2(0,-300);catalog.offsetMax=Vector2.zero;
                controls.SetParent(core,false);controls.anchorMin=Vector2.zero;controls.anchorMax=new Vector2(1,0);controls.offsetMin=Vector2.zero;controls.offsetMax=new Vector2(0,350);
            }

            for(int i=0;i<7;i++){
                t.particleEffects[i]=AssetDatabase.LoadAssetAtPath<GameObject>(PathAt(i));t.particleBounds[i]=Measure(t.particleEffects[i],out t.particleDurations[i]);
                string name="Common_"+Ids[i];var found=catalog.Find(name);var b=found?found.GetComponent<Button>():UnityEngine.Object.Instantiate(template,catalog);b.name=name;b.GetComponentInChildren<Text>().text=Names[i];buttons.Add(b);
            }
            t.selectionButtons=buttons.ToArray();
            for(int i=0;i<buttons.Count;i++){var rect=(RectTransform)buttons[i].transform;rect.anchoredPosition=new Vector2(22+i%4*171,-(102+i/4*59));rect.sizeDelta=new Vector2(163,51);var label=buttons[i].GetComponentInChildren<Text>();label.fontSize=18;var lr=label.rectTransform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;}
            // Fit the fixed HUD across 3:4 as well as the narrow portrait viewport.
            var scaler=root.GetComponentInChildren<CanvasScaler>();scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            t.title.fontSize=17;t.effectAnchor.localPosition=new Vector3(0,.1f,0);
            PrefabUtility.SaveAsPrefabAsset(root,SpineEffectsTestBuilder.Root+"/SpineEffectsTestRig.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}

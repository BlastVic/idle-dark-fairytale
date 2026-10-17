using System;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

namespace RedHoodPrototype.Editor {
    public static class RedHoodWeaponViews {
        const string Root="Assets/RedHoodPrototype/";
        [MenuItem("Tools/Red Hood/Install Weapon Views (Explicit)")]
        public static void Install() {
            AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"RedHoodWeaponViews.png");
            importer.textureType=TextureImporterType.Default;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            // Use the Spine importer's canonical material instead of a duplicate
            // that can be orphaned when the atlas is imported again.
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"RedHoodWeaponViews_Material.mat");
            if (!material) {
                material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"RedHood_Material.mat"));
                AssetDatabase.CreateAsset(material,Root+"RedHoodWeaponViews_Material.mat");
            }
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"RedHoodWeaponViews.png");
            material.SetFloat("_StraightAlphaInput",1);
            material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
            EditorUtility.SetDirty(material);
            var atlas=AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(Root+"RedHoodWeaponViews_Atlas.asset");
            if (!atlas) {
                atlas=ScriptableObject.CreateInstance<SpineAtlasAsset>();
                AssetDatabase.CreateAsset(atlas,Root+"RedHoodWeaponViews_Atlas.asset");
            }
            atlas.atlasFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"RedHoodWeaponViews.atlas.txt");
            atlas.materials=new[]{material}; atlas.Clear(); EditorUtility.SetDirty(atlas);
            foreach (string path in new[]{Root+"RedHood_SkeletonData.asset","Assets/DarkFairytale/RedHoodBattle_SkeletonData.asset"}) {
                var data=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                var list=new System.Collections.Generic.List<AtlasAssetBase>(data.atlasAssets);
                if (!list.Contains(atlas)) list.Add(atlas);
                data.atlasAssets=list.ToArray(); data.Clear(); EditorUtility.SetDirty(data);
                if (data.GetSkeletonData(false)==null) throw new Exception("Cannot load "+path);
            }
            var white=AssetDatabase.LoadAssetAtPath<Material>(Root+"RedHoodWeaponViews_White.mat");
            if (!white) {
                white=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFairytale/RedHoodWhite.mat"));
                AssetDatabase.CreateAsset(white,Root+"RedHoodWeaponViews_White.mat");
            }
            white.mainTexture=material.mainTexture; EditorUtility.SetDirty(white);
            const string playerPath="Assets/Prefab/Player.prefab";
            var player=PrefabUtility.LoadPrefabContents(playerPath);
            try {
                var flash=player.GetComponentInChildren<MaterialSwapper>(true);
                if (flash.origMats.Count!=flash.whiteMats.Length)
                    throw new Exception("Existing flash material mapping is inconsistent");
                bool changed=false;
                for(int i=0;i<flash.whiteMats.Length;i++)
                    if(flash.whiteMats[i]==white && flash.origMats[i]!=material) {
                        flash.origMats[i]=material; changed=true;
                    }
                if (!flash.origMats.Contains(material)) {
                    flash.origMats.Add(material);
                    var whites=new System.Collections.Generic.List<Material>(flash.whiteMats);
                    whites.Add(white); flash.whiteMats=whites.ToArray();
                    changed=true;
                }
                if(changed)PrefabUtility.SaveAsPrefabAsset(player,playerPath);
            } finally { PrefabUtility.UnloadPrefabContents(player); }
            AssetDatabase.SaveAssets();
            Debug.Log("RED_HOOD_WEAPON_VIEWS_INSTALLED");
        }
    }
}

using System.Collections.Generic;
using Assets.Scripts.Services;
using Scripts.Level;
using Scripts.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayerPrefs = Assets.Scripts.Services.PlayerPrefs;

namespace Melcat
{
    public static class MapPackageCreator
    {
        public const string MAIN_SCENE_PATH = "Assets/Scenes/MainScenes/Gameplay.unity";
        public const string mapPackageAssetPath = "Assets/Prefab/Addressable Prefabs/MapPackageAuto.prefab";
        public const string contentAssetPath = "Canvas/map nodes grid";
        public const string mapNodeAssetPath = "Canvas/MapTemp";
        public const string mapEmptyAssetPath = "Canvas/EmptyTemp";

        [MenuItem("Melcat/Map Pakcage/Create", false, 1)]
        public static void CreateMapPackage()
        {
            DeleteMapNode();
        }

        private static void DeleteMapNode()
        {
            Object obj = AssetDatabase.LoadAssetAtPath(mapPackageAssetPath,typeof(Object));
            if (obj != null)
            {
                var scene = EditorSceneManager.OpenScene(MAIN_SCENE_PATH);
                GameObject goWave = GameObject.Find("WaveManager (KEEP ON)");
                if(goWave != null)
                {
                    GameObject go = GameObject.Instantiate(obj) as GameObject;
                    GameObject content = go.transform.Find(contentAssetPath).gameObject;
                    GameObject tempNode = go.transform.Find(mapNodeAssetPath).gameObject;
                    GameObject tempEmpty = go.transform.Find(mapEmptyAssetPath).gameObject;
                    for (int i = content.transform.childCount - 1; i >= 0; i--)
                    {
                        GameObject.DestroyImmediate(content.transform.GetChild(i).gameObject);
                    }
                    int index = 0;
                    bool existNode = true;
                    while (existNode)
                    {
                        existNode = false;
                        Waveset leftNode = GetWaveInPosition(0, index);
                        if(leftNode != null)
                        {
                            GameObject child = GameObject.Instantiate(tempNode);
                            child.transform.name = leftNode.levelKey;
                            child.transform.parent = content.transform;
                            child.transform.transform.localScale = new Vector3(1f, -1f, 1f);
                            child.GetComponent<MapNode>().UpdateInfo(leftNode);
                            existNode = true;
                        }
                        else
                        {
                            GameObject child = GameObject.Instantiate(tempEmpty);
                            child.transform.name = "Empty";
                            child.transform.parent = content.transform;
                            child.transform.transform.localScale = new Vector3(1f, -1f, 1f);
                        }
                        Waveset rightNode = GetWaveInPosition(1, index);
                        if (rightNode != null)
                        {
                            GameObject child = GameObject.Instantiate(tempNode);
                            child.transform.name = rightNode.levelKey;
                            child.transform.parent = content.transform;
                            child.transform.transform.localScale = new Vector3(1f, -1f, 1f);
                            child.GetComponent<MapNode>().UpdateInfo(rightNode);
                            existNode = true;
                        }
                        else
                        {
                            GameObject child = GameObject.Instantiate(tempEmpty);
                            child.transform.name = "Empty";
                            child.transform.parent = content.transform;
                            child.transform.transform.localScale = new Vector3(1f, -1f, 1f);
                        }
                        index++;
                    }
                    
                    PrefabUtility.SaveAsPrefabAsset(go, mapPackageAssetPath);
                    GameObject.DestroyImmediate(go);
                    EditorUtility.SetDirty(obj);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        private static Waveset GetWaveInPosition(int x,int y)
        {
            GameObject goWave = GameObject.Find("WaveManager (KEEP ON)");
            WaveManager manager = goWave.GetComponent<WaveManager>();
            Waveset wave = null;
            foreach(var child in manager.waveDb)
            {
                if (child.position == x + "," + y)
                {
                    wave = child;
                    break;
                }
            }
            return wave;
        }
    }
}

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Extentions.ResourceAsset
{
    public static class ScriptableObjectAsset
    {
        /// <summary>
        /// This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateAsset<T>() where T : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (path == "")
                path = "Assets\\Resources\\";
            else if (Path.GetExtension(path) != "")
                path = path.Replace(Path.GetFileName(path), "") + "\\";

            T asset = ScriptableObject.CreateInstance<T>();
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + typeof(T).Name + ".asset");
            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            return asset;
        }
    }
}
#endif
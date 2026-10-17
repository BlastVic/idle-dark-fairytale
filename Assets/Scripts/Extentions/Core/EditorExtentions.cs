#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Assets.Scripts.Extentions
{
    public static class EditorExtentions
    {
        /// <summary>
        /// Returns relative filepath of the existing assets.
        /// </summary>
        /// <param name="requestedFileName"></param>
        /// <param name="targetDirectory">omit this parameter to start search from the root project folder</param>
        /// <returns>null if no existing file were found</returns>
        public static String FindAssetPath(String requestedFileName, String targetDirectory = null)
        {
            if (targetDirectory == null)
                targetDirectory = Application.dataPath;

            // Process the list of files found in the directory.
            var fileEntries = Directory.GetFiles(targetDirectory);
            foreach (var fileName in fileEntries)
                if (Path.GetFileName(fileName) == requestedFileName)
                {
                    var directoryName = Path.GetDirectoryName(fileName);
                    if (directoryName == null)
                        return null;

                    var resourcesStartIndex = directoryName.IndexOf("Resources", StringComparison.InvariantCulture);
                    if (resourcesStartIndex < 0)
                    {
                        Debug.LogError(String.Format("Asset named {0} was found at {1}. Unity will be unable to load it. Please place it inside 'Resource' folder or in any of it's subdirectories.", requestedFileName, directoryName));
                        return null;
                    }
                    if (resourcesStartIndex + "Resources".Length + 1 < directoryName.Length)
                        directoryName =
                            directoryName.Substring(resourcesStartIndex + "Resources".Length + 1).Replace('\\', '/') + '/'; //+1 removes extra '/'
                    else
                        directoryName = "";

                    var result = directoryName + Path.GetFileNameWithoutExtension(fileName);
                    return result;
                }

            // Recurse into subdirectories of this directory.
            var subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (var subdirectory in subdirectoryEntries)
            {
                var result = FindAssetPath(requestedFileName, subdirectory);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static Type[] GetAllDerivedTypes(this AppDomain aAppDomain, Type aType)
        {
            var assemblies = aAppDomain.GetAssemblies();
            return (from assembly in assemblies
                    from type in assembly.GetTypes()
                    where type.IsSubclassOf(aType)
                    select type).ToArray();
        }

        public static Rect GetEditorMainWindowPos()
        {
            var containerWinType = AppDomain.CurrentDomain.GetAllDerivedTypes(typeof(ScriptableObject)).FirstOrDefault(t => t.Name == "ContainerWindow");
            if (containerWinType == null)
                throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
            var showModeField = containerWinType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
            var positionProperty = containerWinType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
            if (showModeField == null || positionProperty == null)
                throw new MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
            var windows = Resources.FindObjectsOfTypeAll(containerWinType);
            foreach (var win in windows)
            {
                var showmode = (int)showModeField.GetValue(win);
                if (showmode == 4) // main window
                {
                    var pos = (Rect)positionProperty.GetValue(win, null);
                    return pos;
                }
            }
            throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
        }

        public static void CenterOnMainWindow(this EditorWindow aWin)
        {
            var main = GetEditorMainWindowPos();
            var pos = aWin.position;
            float w = (main.width - pos.width) * 0.5f;
            float h = (main.height - pos.height) * 0.5f;
            pos.x = main.x + w;
            pos.y = main.y + h;
            aWin.position = pos;
        }

        public static Boolean SetValue<TVariableType>(this Object targetObject, String variableName,
            TVariableType value, Boolean declaredOnly = false, Boolean ignoreMissingVariable = false, Boolean forceSet = false,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            Object[] index = null)
        {
            if (declaredOnly)
                bindingFlags = bindingFlags | BindingFlags.DeclaredOnly;

            var targetType = targetObject.GetType();
            while (targetType != null)
            {
                var fieldInfo = targetType.GetFields(bindingFlags).FirstOrDefault(fi => fi.Name == variableName);
                if (fieldInfo != null)
                {
                    var currentValue = fieldInfo.GetValue(targetObject);
                    if (!forceSet && Equals(currentValue, value))
                        return false;

                    fieldInfo.SetValue(targetObject, value);
                }
                else
                {
                    var propertyInfo = targetType.GetProperties(bindingFlags).FirstOrDefault(pi => pi.Name == variableName);
                    if (propertyInfo != null)
                    {
                        var currentValue = propertyInfo.GetValue(targetObject, index);
                        if (!forceSet && Equals(currentValue, value))
                            return false;

                        propertyInfo.SetValue(targetObject, value, index);
                    }
                    else
                    {
                        if (ignoreMissingVariable)
                            return false;

                        Debug.LogWarning("Unable to find " + variableName);
                        return false;
                    }
                }

                targetType = targetType.BaseType;
            }

            return true;
        }

        public static TVariableType GetValue<TVariableType>(this Object targetObject, String variableName,
            Boolean declaredOnly = false, Boolean ignoreMissingVariable = false,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            Object[] index = null)
        {
            if (declaredOnly)
                bindingFlags = bindingFlags | BindingFlags.DeclaredOnly;

            Object result = null;
            var targetType = targetObject.GetType();

            while (targetType != null)
            {
                var fieldInfo = targetType.GetFields(bindingFlags).FirstOrDefault(fi => fi.Name == variableName);
                if (fieldInfo != null)
                {
                    result = fieldInfo.GetValue(targetObject);
                    break;
                }

                var propertyInfo = targetType.GetProperties(bindingFlags).FirstOrDefault(pi => pi.Name == variableName);
                if (propertyInfo != null)
                {
                    result = propertyInfo.GetValue(targetObject, index);
                    break;
                }

                targetType = targetType.BaseType;
            }

            if (result is TVariableType)
                return (TVariableType)result;

            if (!ignoreMissingVariable)
                Debug.LogError(String.Format("Expected type for {0} is {1}. Reflected type is {2}.", variableName,
                    typeof(TVariableType).Name, result == null ? "'Unable to get type, variable is null'" : result.GetType().Name));

            return default(TVariableType);
        }

        /// <summary>
        /// Reflects all visible fields and properties of the targetObject and checks is one with the provided name available
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="variableName"></param>
        /// <param name="bindingFlags"></param>
        /// <returns>true if a field or a property with provided name was found</returns>
        public static Boolean HasValue(this Object targetObject, String variableName,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            return targetObject.GetType().GetFields(bindingFlags).FirstOrDefault(field => field.Name == variableName) != null ||
                   targetObject.GetType().GetProperties(bindingFlags).FirstOrDefault(field => field.Name == variableName) != null;
        }

        /// <summary>
        /// Reflects all visible methods of the targetObject and executes one with the provided methodName if possible
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="methodName"></param>
        /// <param name="bindingFlags"></param>
        /// <param name="arguments"></param>
        public static void ExecuteMethod(this Object targetObject, String methodName,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            params Object[] arguments)
        {
            var method = targetObject.GetType()
                .GetMethods(bindingFlags)
                .FirstOrDefault(m => m.Name == methodName);

            if (method != null)
                method.Invoke(targetObject, arguments);
        }

        public static Object GetPropertyObject(this SerializedProperty property)
        {
            var splitedPath = property.propertyPath.Replace(".Array.data[", "[").Split('.');
            Object target = property.serializedObject.targetObject;
            foreach (var element in splitedPath)
            {
                if (element.Contains("["))
                {
                    var split = element.Split('[');
                    var elementName = split[0];
                    var index = Convert.ToInt32(split[1].Substring(0, split[1].Length - 1));
                    target = target.GetValue<Array>(elementName).GetValue(index);
                }
                else
                {
                    target = target.GetValue<Object>(element);
                }
            }
            return target;
        }

        public static Boolean IsSceneObject(UnityEngine.Object targetObject)
        {
            return targetObject is Component
                ? !EditorUtility.IsPersistent((targetObject as Component).transform.root.gameObject)
                : targetObject is GameObject
                    ? !EditorUtility.IsPersistent((targetObject as GameObject).transform.root.gameObject)
                    : false;
        }

        public static void MarkActiveSceneAsDirty()
        {
            if (EditorApplication.isPlaying)
                return;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Get the name of a static or instance property from a property access lambda.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="propertyLambda">lambda expression of the form: '() => Class.Property' or '() => object.Property'</param>
        /// <returns>The name of the property</returns>
        public static String GetPropertyName<T>(Expression<Func<T>> propertyLambda)
        {
            var me = propertyLambda.Body as MemberExpression;

            if (me == null)
            {
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
            }

            return me.Member.Name;
        }

        public class AssetProcesingHelper
        {
            public Type Type { get; private set; }
            public Action<UnityEngine.Object> ProcessingAction { get; private set; }

            public AssetProcesingHelper(Type type, Action<UnityEngine.Object> action)
            {
                Type = type;
                ProcessingAction = action;
            }
        }

        public static IEnumerable<Single> ProcessAssetsOfTypesAll(IList<AssetProcesingHelper> procesingHelpers)
        {
            var files = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; ++i)
            {
                if (!files[i].EndsWith(".meta"))
                {
                    foreach (var procesingHelper in procesingHelpers)
                    {
                        if (procesingHelper.Type.IsSubclassOf(typeof(Component)))
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(files[i].Substring(Application.dataPath.Length - "Assets".Length));
                            if (asset == null)
                            {
                                continue;
                            }

                            foreach (var component in asset.GetComponentsInChildren(procesingHelper.Type, true))
                            {
                                if (component == null)
                                {
                                    continue;
                                }

                                procesingHelper.ProcessingAction.SafeInvoke(component);
                            }
                        }
                        else
                        {
                            var asset = AssetDatabase.LoadAssetAtPath(files[i].Substring(Application.dataPath.Length - "Assets".Length), procesingHelper.Type);

                            if (asset == null)
                            {
                                continue;
                            }

                            procesingHelper.ProcessingAction.SafeInvoke(asset);
                        }
                    }
                }

                yield return i / (Single)files.Length;
            }
        }

        public static IEnumerable<Single> ProcessSceneObjectsOfTypesAll(IList<AssetProcesingHelper> procesingHelpers)
        {
            foreach (var procesingHelper in procesingHelpers)
            {
                if (!procesingHelper.Type.IsSubclassOf(typeof(Component)))
                {
                    continue;
                }

                for (var i = 0; i < SceneManager.sceneCount; ++i)
                {
                    yield return 0f;
                    foreach (var component in SceneManager.GetSceneAt(i).GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren(procesingHelper.Type, true)))
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        procesingHelper.ProcessingAction.SafeInvoke(component);
                    }

                    yield return i / (Single)SceneManager.sceneCount;
                }
            }
        }

        public static IEnumerable<Single> ProcessPrefabSceneObjectsOfTypesAll(IList<AssetProcesingHelper> procesingHelpers)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                yield break;
            }

            foreach (var procesingHelper in procesingHelpers)
            {
                if (!procesingHelper.Type.IsSubclassOf(typeof(Component)))
                {
                    continue;
                }

                yield return 0f;
                foreach (var component in stage.prefabContentsRoot.GetComponentsInChildren(procesingHelper.Type, true))
                {
                    if (component == null)
                    {
                        continue;
                    }

                    procesingHelper.ProcessingAction.SafeInvoke(component);
                }
            }
        }

        public static IEnumerable<T> FindObjectsOfTypeAll<T>() where T : UnityEngine.Object
        {
            var files = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                if (typeof(T).IsSubclassOf(typeof(Component)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(file.Substring(Application.dataPath.Length - "Assets".Length));
                    if (asset == null)
                    {
                        continue;
                    }

                    foreach (var component in asset.GetComponentsInChildren<T>(true))
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        yield return component;
                    }
                }
                else
                {
                    var asset = AssetDatabase.LoadAssetAtPath<T>(file.Substring(Application.dataPath.Length - "Assets".Length));

                    if (asset == null)
                    {
                        continue;
                    }

                    yield return asset;
                }
            }
        }

        public static IEnumerable<UnityEngine.Object> FindAssetsOfTypeAll(Type type)
        {
            var files = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }


                if (type.IsSubclassOf(typeof(Component)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(file.Substring(Application.dataPath.Length - "Assets".Length));
                    if (asset == null)
                    {
                        continue;
                    }

                    foreach (var component in asset.GetComponentsInChildren(type, true))
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        yield return component;
                    }
                }
                else
                {
                    var asset = AssetDatabase.LoadAssetAtPath(file.Substring(Application.dataPath.Length - "Assets".Length), type);

                    if (asset == null)
                    {
                        continue;
                    }

                    yield return asset;
                }
            }
        }

        public static IEnumerable<T> FindSceneObjectsOfTypeAll<T>() where T : UnityEngine.Object
        {
            if (!typeof(T).IsSubclassOf(typeof(Component)))
            {
                yield break;
            }

            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                foreach (var component in SceneManager.GetSceneAt(i).GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)))
                {
                    if (component == null)
                    {
                        continue;
                    }

                    yield return component;
                }
            }

            if (EditorApplication.isPlaying)
            {
                GameObject temp = null;
                try
                {
                    temp = new GameObject();
                    UnityEngine.Object.DontDestroyOnLoad(temp);
                    foreach (var component in temp.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)))
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        yield return component;
                    }

                    UnityEngine.Object.DestroyImmediate(temp);
                    temp = null;
                }
                finally
                {
                    if (temp != null)
                    {
                        UnityEngine.Object.DestroyImmediate(temp);
                    }
                }
            }
        }

        public static IEnumerable<Component> FindSceneObjectsOfTypeAll(Type type)
        {
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                foreach (var component in SceneManager.GetSceneAt(i).GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren(type, true)))
                {
                    yield return component;
                }
            }
        }

        public static void SetIcon(GameObject gObj, Texture2D texture)
        {
            var ty = typeof(EditorGUIUtility);
            var mi = ty.GetMethod("SetIconForObject", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo SetIconForObject = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);
            mi.Invoke(null, new object[] { gObj, texture });
        }

        private static readonly Dictionary<MonoBehaviour, MonoScript> _monoScripts = new Dictionary<MonoBehaviour, MonoScript>();

        public static void DrawScriptField(MonoBehaviour target)
        {
            if (!_monoScripts.ContainsKey(target))
            {
                _monoScripts[target] = MonoScript.FromMonoBehaviour(target);
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", _monoScripts[target], typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();
        }

        public static String GetFullObjectPath(this Transform target)
        {
            var assetPath = AssetDatabase.GetAssetPath(target.gameObject);
            assetPath = assetPath.Substring(0, assetPath.Length - ".prefab".Length);
            return $"{assetPath}/{target.GetObjectPath(false)}";
        }
    }
}
#endif
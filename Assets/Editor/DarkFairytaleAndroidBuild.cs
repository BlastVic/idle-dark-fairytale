using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

[InitializeOnLoad]
public static class DarkFairytaleAndroidBuild
{
    const string Output = "output/apk-comparison/";
    static DarkFairytaleAndroidBuild() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        if (!File.Exists(Output + "build.request")) return;
        File.Delete(Output + "build.request");
        try { Build(); }
        catch (Exception e) { File.WriteAllText(Output + "failure.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Dark Fairytale/Build Android APK")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);
        File.Delete(Output + "success.txt");
        File.Delete(Output + "failure.txt");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play mode before building.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) throw new Exception("Android must be the active build target.");
        var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        if (playerSettings.FindProperty("activeInputHandler").intValue != 0)
            throw new Exception("This project's UI and controls require Input Manager (Old). Android must not use Both.");
        for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save modified scenes before building.");
        var previous = BuildReport.GetLatestReport();
        var options = BuildOptions.None;
        if (previous != null && previous.summary.platform == BuildTarget.Android)
        {
            options = previous.summary.options & (BuildOptions.CompressWithLz4 | BuildOptions.CompressWithLz4HC);
            File.WriteAllText(Output+"previous-build.txt", "Output: "+previous.summary.outputPath+"\nOptions: "+previous.summary.options+"\n");
        }
        Directory.CreateDirectory("Build/Android");
        string apk = "Build/Android/IdleDarkFairytale-"+PlayerSettings.bundleVersion+"-"+DateTime.Now.ToString("yyyyMMddHHmm")+".apk";
        if(File.Exists(apk)) throw new Exception("Output already exists: "+apk);
        bool bundle=EditorUserBuildSettings.buildAppBundle;
        bool export=EditorUserBuildSettings.exportAsGoogleAndroidProject;
        try
        {
            EditorUserBuildSettings.buildAppBundle=false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            File.WriteAllText(Output+"status.txt","Preparing single-copy local prefabs");
            LocalPrefabPackaging.Prepare();
            File.WriteAllText(Output+"status.txt","Building APK: "+apk);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=apk,target=BuildTarget.Android,options=options
            });
            File.WriteAllText(Output+"build-report.txt","Result: "+report.summary.result+"\nOptions: "+report.summary.options+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nDuration: "+report.summary.totalTime+"\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
            File.WriteAllText(Output+"success.txt",Path.GetFullPath(apk));
            File.WriteAllText(Output+"status.txt","Succeeded");
        }
        finally { EditorUserBuildSettings.buildAppBundle=bundle;EditorUserBuildSettings.exportAsGoogleAndroidProject=export; }
    }
}

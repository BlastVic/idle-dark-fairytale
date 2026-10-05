#if UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Assets.Editor.BuildPostprocess
{
    internal static class xCodeConfigurator
    {
        [PostProcessBuild]
        public static void OnBuilt(BuildTarget buildTarget, String path)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                UpdateInfoPlist(path);
            }
        }

        private static void UpdateInfoPlist(String path)
        {
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict rootDict = plist.root;

            //According to Admob's requirements, please add your application ID to infoPlist
            rootDict.SetString("GADApplicationIdentifier", "ca-app-pub-7773639884637657~8747259188");

            //In order to adapt to ios 14 to control idfa, you need to add SKAdNetwork settings for each advertising platform
            if (!rootDict.values.ContainsKey("SKAdNetworkItems"))
            {
                rootDict.CreateArray("SKAdNetworkItems");
            }
            //IronSource
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "su67r6k2v3.skadnetwork");
            //Admob
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "cstr6suwn9.skadnetwork");
            //AppLovin
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "ludvb6z3bs.skadnetwork");
            //Facebook
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "v9wttpbfk9.skadnetwork");
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "n38lu8286q.skadnetwork");
            //Pangle
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "22mmun2rn5.skadnetwork");
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "238da6jt44.skadnetwork");
            //Unity
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "4dzt52r2t5.skadnetwork");
            //Vungle
            rootDict["SKAdNetworkItems"].AsArray().AddDict().SetString("SKAdNetworkIdentifier", "gta9lk7p23.skadnetwork");

            // Write plist
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
#endif
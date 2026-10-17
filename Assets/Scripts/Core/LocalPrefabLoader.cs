using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LocalPrefabLoader
{
    public const string ResourceFolder = "LocalPrefabs";

    public static AsyncOperationHandle<GameObject> InstantiateAsync(AssetReference reference, Vector3 position, Quaternion rotation)
    {
        return Addressables.ResourceManager.StartOperation(new InstantiateOperation(reference == null ? null : reference.AssetGUID, position, rotation), default);
    }

    public static void Release(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.IsValid()) Addressables.ResourceManager.Release(handle);
    }

    public static void ReleaseInstance(AsyncOperationHandle<GameObject> handle)
    {
        if (!handle.IsValid()) return;
        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result) UnityEngine.Object.Destroy(handle.Result);
        Release(handle);
    }

    sealed class InstantiateOperation : AsyncOperationBase<GameObject>
    {
        readonly string guid;
        readonly Vector3 position;
        readonly Quaternion rotation;
        public InstantiateOperation(string guid, Vector3 position, Quaternion rotation)
        { this.guid = guid; this.position = position; this.rotation = rotation; }

        protected override void Execute()
        {
            if (string.IsNullOrEmpty(guid)) { Complete(null, false, "Missing local prefab GUID"); return; }
            var request = Resources.LoadAsync<LocalPrefabEntry>(ResourceFolder + "/" + guid);
            request.completed += _ => {
                try {
                    var entry = request.asset as LocalPrefabEntry;
                    if (!entry || !entry.prefab) throw new InvalidOperationException("Missing local prefab entry: " + guid);
                    Complete(UnityEngine.Object.Instantiate(entry.prefab, position, rotation), true, (string)null);
                }
                catch (Exception e) { Complete(null, false, e.Message); }
            };
        }
    }
}

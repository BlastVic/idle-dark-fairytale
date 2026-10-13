using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Serialized catalog row. Its portrait and label can be edited in Prefab Mode.</summary>
public sealed class MonsterPreviewCard : MonoBehaviour
{
    public SkeletonDataAsset data;
    public string displayName;
    public Image portrait;
    public Text title;
    public Button button;
    public Image selection;
}

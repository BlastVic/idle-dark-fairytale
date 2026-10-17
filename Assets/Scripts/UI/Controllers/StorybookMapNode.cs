using UnityEngine;
using UnityEngine.UI;

/// <summary>Saved editable visual node; its key refers to existing wave data.</summary>
public sealed class StorybookMapNode : MonoBehaviour
{
    public enum NodeKind { Battle, Story }
    public NodeKind kind;
    public StorySequence story;
    public CursedBookIntro storyPlayerOverride;
    public string storyUnlockLevelKey;
    public bool IsStory => kind == NodeKind.Story;
    public string levelKey;
    public string displayName;
    public Button button;
    public Image seal, labelFrame;
    public Text label, progress;
    public GameObject locator, labelRoot;
    public Sprite preview, customSeal;
    public Material customMaterial;
    public GameObject lockBadge;
    public Image[] incomingRoute;
}

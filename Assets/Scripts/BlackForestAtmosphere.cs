using Spine.Unity;
using UnityEngine;

/// <summary>Layers share the background's framing, including camera movement and aspect changes.</summary>
public sealed class BlackForestAtmosphere : MonoBehaviour
{
    public void Initialize(DarkFairytaleBattleStyle style)
    {
        var backdrop = GetComponent<SpriteRenderer>().sprite;
        if (style.foreground)
        {
            var go = new GameObject("Foreground Reeds", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = style.foreground;
            renderer.sortingLayerName = "Character_Front";
            renderer.sortingOrder = 100;
            go.transform.localScale = new Vector3(backdrop.bounds.size.x / style.foreground.bounds.size.x,
                backdrop.bounds.size.y / style.foreground.bounds.size.y, 1);
        }
        // Coordinates use the art's original 1024 x 1536 canvas (origin at top left).
        AddProp("Lantern Left", style.lanternAtmosphere, new Vector2(110, 386), 1, 0, backdrop);
        AddProp("Lantern Right", style.lanternAtmosphere, new Vector2(914, 401), -.95f, 3.7f, backdrop);
        AddProp("Lantern Distant Left", style.lanternAtmosphere, new Vector2(277, 632), -.63f, 7.2f, backdrop);
        AddProp("Lantern Distant Right", style.lanternAtmosphere, new Vector2(747, 632), .60f, 11.1f, backdrop);
        AddProp("Watching Moon", style.moonAtmosphere, new Vector2(725, 185), 1, 0, backdrop);
    }

    void AddProp(string label, SkeletonDataAsset data, Vector2 pixel, float scale, float phase, Sprite backdrop)
    {
        if (!data) return;
        var go = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3((pixel.x / 1024f - .5f) * backdrop.bounds.size.x,
            (.5f - pixel.y / 1536f) * backdrop.bounds.size.y, 0);
        float unitsPerPixel = backdrop.bounds.size.x / 1024f;
        go.transform.localScale = new Vector3(scale * unitsPerPixel / data.scale,
            Mathf.Abs(scale) * unitsPerPixel / data.scale, 1);
        var skeleton = go.AddComponent<SkeletonAnimation>();
        skeleton.skeletonDataAsset = data;
        skeleton.Initialize(true);
        skeleton.AnimationName = "Ambient";
        skeleton.loop = true;
        var track = skeleton.AnimationState.SetAnimation(0, "Ambient", true);
        track.TrackTime = phase;
        if (skeleton.Skeleton.FindBone("swing") != null)
            go.AddComponent<BlackForestLanternMotion>().Initialize(System.Guid.NewGuid().GetHashCode());
        else if (skeleton.Skeleton.FindBone("pupil") != null)
            go.AddComponent<BlackForestMoonMotion>().Initialize(System.Guid.NewGuid().GetHashCode());
        skeleton.Update(0);
        skeleton.LateUpdate();
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "BG";
        renderer.sortingOrder = -90;
    }
}

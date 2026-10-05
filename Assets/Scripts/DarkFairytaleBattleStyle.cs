using Spine.Unity;
using UnityEngine;

/// <summary>Presentation only: viewport positions keep the battle clear of the HUD.</summary>
[CreateAssetMenu(menuName = "Dark Fairytale/Battle Style")]
public class DarkFairytaleBattleStyle : ScriptableObject
{
    public Sprite background;
    public Sprite contactShadow;
    public Vector2 playerFeet = new Vector2(.255f, .30f);
    public Vector2 playerSize = new Vector2(.47f, .28f);
    public Vector2[] enemyFeet = {
        new Vector2(.70f, .365f), new Vector2(.81f, .30f),
        new Vector2(.78f, .51f), new Vector2(.57f, .46f)
    };
    public Vector2[] enemySize = {
        new Vector2(.35f, .28f), new Vector2(.29f, .20f),
        new Vector2(.27f, .20f), new Vector2(.23f, .17f)
    };

    public GameObject CreateBackdrop(Camera camera)
    {
        var go = new GameObject("Moonlit Castle Battle", typeof(SpriteRenderer), typeof(DarkFairytaleBackdrop));
        go.tag = "MapGraphics";
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = background;
        renderer.sortingLayerName = "BG";
        renderer.sortingOrder = -100;
        go.GetComponent<DarkFairytaleBackdrop>().Initialize(camera);
        return go;
    }

    public Vector3 EnemyPosition(Camera camera, int index)
    {
        return ViewportPoint(camera, enemyFeet[Mathf.Clamp(index, 0, enemyFeet.Length - 1)]);
    }

    public void PlacePlayer(Player player, Camera camera)
    {
        Fit(player.transform, player.skeletonAnimation, camera, playerFeet, playerSize);
        SetShadow(player.transform, player.skeletonAnimation, camera, playerFeet);
    }

    public void PlaceEnemy(Enemy enemy, Camera camera, int index)
    {
        // Addressables can complete before Enemy.Start assigns its animation controller.
        if (!enemy.skeletonAnimation)
            enemy.skeletonAnimation = enemy.GetComponentInChildren<SkeletonAnimation>();
        index = Mathf.Clamp(index, 0, enemyFeet.Length - 1);
        var drop = enemy.GetComponent<DropIn>();
        if (drop) drop.ResetForLayout();
        var bar = enemy.ab.enemyLifebar;
        bool boss = bar && (bar.isBoss || bar.isMiniBoss);
        var feet = boss ? new Vector2(.72f, .33f) : enemyFeet[index];
        var size = boss ? new Vector2(.40f, .34f) : enemySize[index];
        Fit(enemy.transform, enemy.skeletonAnimation, camera, feet, size);
        var bounds = enemy.skeletonAnimation.GetComponent<MeshRenderer>().bounds;
        if (bar)
        {
            bar.GetComponent<Canvas>().worldCamera = camera;
            bar.transform.position = new Vector3(bounds.center.x, bounds.max.y + .28f, 0);
            // Existing lifebars have a 250px meter nested under a 1.5x container.
            float width = Mathf.Min(bounds.size.x, WorldSize(camera).x * .23f);
            float currentWidth = Mathf.Abs(bar.transform.lossyScale.x) * 375f;
            if (currentWidth > .001f) bar.transform.localScale *= width / currentWidth;
        }
        SetShadow(enemy.transform, enemy.skeletonAnimation, camera, feet);
        if (drop) drop.BeginDrop();
    }

    static void Fit(Transform actor, SkeletonAnimation skeleton, Camera camera, Vector2 feet, Vector2 size)
    {
        skeleton.Initialize(false);
        skeleton.AnimationState.ClearTracks();
        skeleton.Skeleton.SetToSetupPose();
        skeleton.Update(0);
        skeleton.LateUpdate();
        var renderer = skeleton.GetComponent<MeshRenderer>();
        var bounds = renderer.bounds;
        var screen = WorldSize(camera);
        float scale = Mathf.Min(screen.x * size.x / Mathf.Max(.01f, bounds.size.x),
            screen.y * size.y / Mathf.Max(.01f, bounds.size.y));
        skeleton.transform.localScale *= scale;
        bounds = renderer.bounds;
        actor.position += ViewportPoint(camera, feet) - new Vector3(bounds.center.x, bounds.min.y, 0);
        renderer.sortingLayerName = "Character";
        renderer.sortingOrder = Mathf.RoundToInt((1f - feet.y) * 100);
    }

    void SetShadow(Transform actor, SkeletonAnimation skeleton, Camera camera, Vector2 feet)
    {
        var shadow = actor.Find("Battle Contact Shadow");
        if (!shadow)
        {
            shadow = new GameObject("Battle Contact Shadow", typeof(SpriteRenderer)).transform;
            shadow.SetParent(actor, false);
        }
        shadow.gameObject.SetActive(true);
        var renderer = shadow.GetComponent<SpriteRenderer>();
        renderer.sprite = contactShadow;
        renderer.sortingLayerName = "BG2";
        renderer.sortingOrder = 0;
        shadow.position = ViewportPoint(camera, feet);
        float width = skeleton.GetComponent<MeshRenderer>().bounds.size.x * .72f;
        shadow.localScale = new Vector3(width / contactShadow.bounds.size.x / Mathf.Abs(actor.lossyScale.x),
            width * .24f / contactShadow.bounds.size.y / Mathf.Abs(actor.lossyScale.y), 1);
    }

    public static Vector2 WorldSize(Camera camera)
    {
        float distance = Mathf.Abs(camera.transform.position.z);
        float height = camera.orthographic ? 2 * camera.orthographicSize :
            2 * distance * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
        return new Vector2(height * camera.aspect, height);
    }

    public static Vector3 ViewportPoint(Camera camera, Vector2 point)
    {
        var world = camera.ViewportToWorldPoint(new Vector3(point.x, point.y, Mathf.Abs(camera.transform.position.z)));
        world.z = 0;
        return world;
    }
}

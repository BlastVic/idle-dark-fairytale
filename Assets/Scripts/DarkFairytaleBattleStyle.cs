using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation only: viewport positions keep the battle clear of the HUD.</summary>
[CreateAssetMenu(menuName = "Dark Fairytale/Battle Style")]
public class DarkFairytaleBattleStyle : ScriptableObject
{
    public Sprite background;
    public Sprite foreground;
    public SkeletonDataAsset lanternAtmosphere;
    public SkeletonDataAsset moonAtmosphere;
    public Sprite contactShadow;
    [Tooltip("Optional authored UGUI prefab, shared by normal and boss health displays.")]
    public EnemyLifebar enemyHealthBarPrefab;
    public Vector2 playerFeet = new Vector2(.23f, .30f);
    public Vector2 playerSize = new Vector2(.35f, .235f);
    public Vector2[] enemyFeet = {
        new Vector2(.68f, .31f), new Vector2(.48f, .45f),
        new Vector2(.73f, .54f), new Vector2(.50f, .48f)
    };
    public Vector2[] enemySize = {
        new Vector2(.24f, .185f), new Vector2(.18f, .14f),
        new Vector2(.205f, .16f), new Vector2(.18f, .14f)
    };

    public GameObject CreateBackdrop(Camera camera)
    {
        var go = new GameObject("Moonlit Castle Battle", typeof(SpriteRenderer), typeof(DarkFairytaleBackdrop));
        go.tag = "MapGraphics";
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = background;
        renderer.sortingLayerName = "BG";
        renderer.sortingOrder = -100;
        if (foreground || lanternAtmosphere || moonAtmosphere)
            go.AddComponent<BlackForestAtmosphere>().Initialize(this);
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
        if (bar && enemyHealthBarPrefab && bar.name != "CursedStorybookEnemyBar")
        {
            var oldBar = bar;
            // Instantiate an editable prefab, not generated UI. Keep child 0 for Actor_Base.Start.
            bar = Instantiate(enemyHealthBarPrefab, enemy.transform, false);
            bar.name = "CursedStorybookEnemyBar";
            bar.isBoss = oldBar.isBoss;
            bar.isMiniBoss = oldBar.isMiniBoss;
            bar.miniBossName.text = boss ? enemy.bossName : "";
            bar.transform.SetSiblingIndex(0);
            enemy.ab.enemyLifebar = bar;
            if (drop) drop.ReplaceLifebar(bar.gameObject);
            oldBar.gameObject.SetActive(false);
            Destroy(oldBar.gameObject);
        }
        var feet = boss ? new Vector2(.72f, .33f) : enemyFeet[index];
        var size = boss ? new Vector2(.40f, .34f) : enemySize[index];
        Fit(enemy.transform, enemy.skeletonAnimation, camera, feet, size);
        var bounds = enemy.skeletonAnimation.GetComponent<MeshRenderer>().bounds;
        if (bar)
        {
            bar.GetComponent<Canvas>().worldCamera = camera;
            // Ground-level labels occupy each actor's own lane instead of crossing
            // the silhouette of a character standing farther back.
            bar.transform.position = boss ? new Vector3(bounds.center.x, bounds.max.y + .28f, 0)
                : ViewportPoint(camera, new Vector2(feet.x, feet.y - .023f));
            // Existing lifebars have a 250px meter nested under a 1.5x container.
            float width = Mathf.Min(bounds.size.x * .55f, WorldSize(camera).x * .15f);
            float currentWidth = Mathf.Abs(bar.transform.lossyScale.x) * (enemyHealthBarPrefab ? 360f : 375f);
            if (currentWidth > .001f) bar.transform.localScale *= width / currentWidth;
            foreach (var graphic in bar.GetComponentsInChildren<Image>(true))
            {
                if (!enemyHealthBarPrefab)
                    graphic.color = graphic.name == "HP FILL" ? new Color(.62f,.29f,.27f) : new Color(.14f,.15f,.20f);
                graphic.raycastTarget = false;
            }
            bar.hpText.color = new Color(.88f,.84f,.73f);
            foreach (var outline in bar.GetComponentsInChildren<Outline>(true))
                outline.effectColor = new Color(.07f,.075f,.105f,.85f);
        }
        SetShadow(enemy.transform, enemy.skeletonAnimation, camera, feet);
        // The same cool ambient wash as the forest, kept subtle enough to retain
        // the ivory stalk and warm eyes. Back-row figures recede slightly.
        enemy.skeletonAnimation.Skeleton.SetColor(index == 0 ? new Color(.92f,.93f,1f) : new Color(.83f,.87f,.96f));
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
        renderer.color = new Color(.12f,.10f,.16f,.65f);
        float width = skeleton.GetComponent<MeshRenderer>().bounds.size.x * .50f;
        shadow.localScale = new Vector3(width / contactShadow.bounds.size.x / Mathf.Abs(actor.lossyScale.x),
            width * .18f / contactShadow.bounds.size.y / Mathf.Abs(actor.lossyScale.y), 1);
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

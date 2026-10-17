using Spine.Unity;
using UnityEngine;

/// <summary>Fits authored scene art and actors into the HUD's safe-area preview slots.</summary>
[ExecuteAlways]
[DefaultExecutionOrder(100)]
public sealed class MonsterPreviewStage : MonoBehaviour
{
    public MonsterAnimationTest tester;
    public MonsterPreviewHud hud;
    public SkeletonAnimation redHood;
    [Tooltip("The same player prefab used by Gameplay. Its complete hierarchy scale calibrates both actors.")]
    public GameObject battlePlayerPrefab;
    public DarkFairytaleBattleStyle battleStyle;
    [Tooltip("0 is the front-row ordinary enemy, matching battle placement.")]
    [Range(0, 3)] public int battleEnemySlot;
    public SpriteRenderer backdrop;
    public Camera previewCamera;
    Bounds monsterBounds, playerBounds, playerSetupBounds;
    Vector3 monsterScale, playerScale;
    bool measured;
    void Start() { InitializePreview(); }
    public void InitializePreview()
    {
        if (redHood)
        {
            redHood.Initialize(false);
            redHood.AnimationState.ClearTracks();redHood.Skeleton.SetToSetupPose();
            redHood.Update(0);redHood.LateUpdate();
            playerSetupBounds = redHood.GetComponent<MeshFilter>().sharedMesh.bounds;
            redHood.AnimationState.SetAnimation(0, "Idle1", true);
            redHood.timeScale = 1;
            redHood.Update(0); redHood.LateUpdate();
            playerBounds = MeasureBasicMotion(redHood, redHood.GetComponent<MeshFilter>().sharedMesh.bounds, "Idle1");
            var source = battlePlayerPrefab ? battlePlayerPrefab.GetComponentInChildren<SkeletonAnimation>(true) : null;
            playerScale = source ? source.transform.lossyScale : Vector3.one;
        }
        MeasureMonster(); Apply();
    }
    void LateUpdate()
    {
        Apply();
        if (tester) tester.ApplyEntrancePreview();
    }
    public void ShowRedHood(bool show) { if (redHood) redHood.gameObject.SetActive(show); }
    public void MeasureMonster()
    {
        if (!tester || !tester.character) return;
        var mesh = tester.character.GetComponent<MeshFilter>().sharedMesh;
        if (!mesh) return;
        var data = tester.character.Skeleton.Data;
        string idle = data.FindAnimation("Idle1") != null ? "Idle1" : data.FindAnimation("idle") != null ? "idle" : null;
        monsterBounds = MeasureBasicMotion(tester.character, tester.CombatSetupBounds, idle);
        monsterScale = tester.CombatDisplayScale;
        measured = true;
    }
    // Sample a separate skeleton so fitting never advances the visible animation or fires events.
    static Bounds MeasureBasicMotion(SkeletonAnimation actor, Bounds fallback, string selectedClip)
    {
        var skeleton = new Spine.Skeleton(actor.Skeleton.Data);
        if (actor.Skeleton.Skin != null) skeleton.SetSkin(actor.Skeleton.Skin.Name);
        Bounds bounds = fallback;
        bool hasPoint = false;
        float[] vertices = new float[8];
        foreach (var name in string.IsNullOrEmpty(selectedClip) ? new string[0] : new[] { selectedClip })
        {
            var animation = skeleton.Data.FindAnimation(name);
            if (animation == null) continue;
            int frames = Mathf.Max(1, Mathf.CeilToInt(animation.Duration * 60));
            for (int frame = 0; frame <= frames; frame++)
            {
                skeleton.SetToSetupPose();
                animation.Apply(skeleton, 0, animation.Duration * frame / frames, false, null, 1, Spine.MixBlend.Replace, Spine.MixDirection.In);
                skeleton.UpdateWorldTransform();
                foreach (var slot in skeleton.DrawOrder)
                {
                    if (slot.A <= 0 || !slot.Bone.Active) continue;
                    int count;
                    if (slot.Attachment is Spine.MeshAttachment mesh)
                    {
                        count = mesh.WorldVerticesLength;
                        if (vertices.Length < count) vertices = new float[count];
                        mesh.ComputeWorldVertices(slot, 0, count, vertices, 0, 2);
                    }
                    else if (slot.Attachment is Spine.RegionAttachment region)
                    {
                        count = 8;
                        region.ComputeWorldVertices(slot.Bone, vertices, 0, 2);
                    }
                    else continue;
                    for (int v = 0; v < count; v += 2) {
                        var point = new Vector3(vertices[v], vertices[v + 1], 0);
                        if (!hasPoint) { bounds = new Bounds(point, Vector3.zero); hasPoint = true; }
                        else bounds.Encapsulate(point);
                    }
                }
            }
        }
        bounds.Expand(new Vector3(bounds.size.x * .04f, bounds.size.y * .04f, 0));
        return bounds;
    }
    public void Apply()
    {
        if (!previewCamera || !hud || !backdrop) return;
        var safe = hud.GetComponent<BattleHudSafeArea>();
        if (safe) safe.Apply();
        previewCamera.orthographicSize = 6.4f;
        previewCamera.transform.position = new Vector3(0,0,-20);
        float height = previewCamera.orthographicSize * 2, width = height * previewCamera.aspect;
        float fit = Mathf.Max(width / backdrop.sprite.bounds.size.x, height / backdrop.sprite.bounds.size.y);
        backdrop.transform.localScale = Vector3.one * fit;
        backdrop.transform.position = Vector3.zero;
        // One world-unit conversion for both actors, calibrated only from the player's battle Idle.
        // Neither monster identity nor the current animation changes the conversion.
        var playerCorners = new Vector3[4]; hud.playerSlot.GetWorldCorners(playerCorners);
        var pa = previewCamera.ScreenToWorldPoint(new Vector3(playerCorners[0].x,playerCorners[0].y,20));
        var pb = previewCamera.ScreenToWorldPoint(new Vector3(playerCorners[2].x,playerCorners[2].y,20));
        var screen = new Vector2(previewCamera.aspect, 1);
        var fittedPlayer = BattleScale(playerScale, playerSetupBounds, screen, battleStyle ? battleStyle.playerSize : Vector2.one);
        var enemySize = battleStyle ? battleStyle.EnemyDisplaySize(battleEnemySlot, tester && tester.CombatIsBoss) : Vector2.one;
        if(tester && tester.CompanionDisplaySize.HasValue) enemySize=tester.CompanionDisplaySize.Value;
        enemySize *= tester ? tester.CombatSizeMultiplier : 1f;
        var fittedMonster = BattleScale(monsterScale, tester ? tester.CombatSetupBounds : monsterBounds, screen, enemySize);
        float units = Mathf.Min((pb.x-pa.x)/Mathf.Max(.01f,playerBounds.size.x*Mathf.Abs(fittedPlayer.x)),
            (pb.y-pa.y)/Mathf.Max(.01f,playerBounds.size.y*Mathf.Abs(fittedPlayer.y))) * (tester ? tester.Zoom : 1);
        if (redHood && playerBounds.size.sqrMagnitude > 0) Place(redHood,playerBounds,fittedPlayer,hud.playerSlot,units);
        if (tester && tester.character && measured) {
            Place(tester.character,monsterBounds,fittedMonster,hud.monsterSlot,units);
            if(tester.CompanionDisplaySize.HasValue && redHood){
                // Allies share the left side; reserve the right side for pet attack effects.
                var actor=tester.character.transform;var scale=actor.lossyScale;
                float center=previewCamera.ViewportToWorldPoint(new Vector3(.20f,.5f,20)).x;
                float feet=redHood.GetComponent<MeshRenderer>().bounds.min.y+height*.07f;
                actor.position=new Vector3(center-monsterBounds.center.x*scale.x,feet-monsterBounds.min.y*scale.y,0);
                tester.character.GetComponent<MeshRenderer>().sortingOrder=redHood.GetComponent<MeshRenderer>().sortingOrder-1;
            }
        }
    }
    Vector3 BattleScale(Vector3 scale, Bounds setup, Vector2 screen, Vector2 size)
    {
        if (!battleStyle) return scale;
        var worldSize = Vector3.Scale(setup.size, new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),1));
        // The same setup-pose fit as DarkFairytaleBattleStyle.Fit, held across every animation.
        return scale * DarkFairytaleBattleStyle.DisplayFitScale(worldSize,screen,size);
    }
    void Place(SkeletonAnimation actor, Bounds bounds, Vector3 facing, RectTransform slot, float zoom)
    {
        var corners = new Vector3[4]; slot.GetWorldCorners(corners);
        Vector3 a = previewCamera.ScreenToWorldPoint(new Vector3(corners[0].x,corners[0].y,20));
        Vector3 b = previewCamera.ScreenToWorldPoint(new Vector3(corners[2].x,corners[2].y,20));
        Vector3 worldScale = facing * zoom;
        var parentScale = actor.transform.parent ? actor.transform.parent.lossyScale : Vector3.one;
        actor.transform.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, 1);
        actor.transform.position = new Vector3((a.x+b.x)*.5f-bounds.center.x*worldScale.x,a.y-bounds.min.y*worldScale.y,0);
    }
}

using System;
using System.Collections.Generic;
using Scripts.Skills;
using UnityEngine;

/// <summary>One cast, five authored runners, one random-target impact per runner.</summary>
[DefaultExecutionOrder(100)]
public sealed class GhostWolfPack : MonoBehaviour
{
    public GhostWolfCharge[] wolves;
    [Tooltip("Fixed enemy-front crossing plane, in battle camera viewport coordinates.")]
    [Range(0, 1)] public float impactViewportX = .60f;
    [Min(.1f)] public float viewportSpeed = 1.05f;
    public float startViewportX = -.40f;
    public int ImpactAttempts { get; private set; }
    public int DamageHits { get; private set; }
    public event Action<GhostWolfCharge, Actor_Base> Impact;
    readonly List<Actor_Base> castTargets = new List<Actor_Base>();
    readonly List<Actor_Base> eligible = new List<Actor_Base>();
    Player owner;
    Camera battleCamera;
    float damage, elapsed;
    bool initialized;

    public static GhostWolfPack Cast(Player player, float compensation = 0)
    {
        if (!player || player.isCampPlayer || player.ab.isDying || player.IsVictoryRelaxing) return null;
        var skills = SkillsController.Instance;
        int level = skills.GetSkillLevel(SkillsTypeEnum.GhostWolfPack);
        if (level <= 0) return null;
        var prefab = Resources.Load<GhostWolfPack>("Effects/GhostWolfPack");
        if (!prefab) { Debug.LogError("Missing authored GhostWolfPack prefab."); return null; }
        var pack = Instantiate(prefab);
        pack.Initialize(player, LevelController.Instance.AssetManager.battleCampCamera.GetComponent<Camera>(),
            player.ab.currentStat.dmg * skills.GetSkillValues(level, SkillsTypeEnum.GhostWolfPack)[0] * skills.GetSummonDamageMultiplier(SkillsTypeEnum.GhostWolfPack) + compensation / pack.wolves.Length);
        return pack;
    }

    public void Initialize(Player player, Camera camera, float hitDamage)
    {
        owner = player; battleCamera = camera; damage = hitDamage;
        // Do not let the tail of a previous cast damage a newly entering wave.
        castTargets.Clear(); castTargets.AddRange(GameManager.single.GetLivingEnemies(false));
        elapsed = 0; ImpactAttempts = DamageHits = 0;
        foreach (var wolf in wolves) wolf.Initialize(camera, startViewportX);
        initialized = true;
    }

    void LateUpdate()
    {
        if (!initialized) return;
        if (!owner || !owner.gameObject.activeInHierarchy || owner.isCampPlayer || owner.ab.isDying || !battleCamera) {
            Destroy(gameObject); return;
        }
        elapsed += Time.deltaTime;
        bool finished = true;
        foreach (var wolf in wolves) {
            if (wolf.Finished) continue;
            wolf.Advance(battleCamera, elapsed, startViewportX, viewportSpeed);
            if (wolf.Started && !wolf.HasImpacted && wolf.MuzzleViewportX(battleCamera) >= impactViewportX) {
                wolf.MarkImpact();
                ApplyImpact(wolf);
            }
            if (!wolf.Finished) finished = false;
        }
        if (finished) Destroy(gameObject);
    }

    void ApplyImpact(GhostWolfCharge wolf)
    {
        ImpactAttempts++;
        eligible.Clear();
        foreach (var target in castTargets) {
            if (!target || !target.gameObject.activeInHierarchy || target.isDying) continue;
            if (target.TryGetComponent<DropIn>(out var drop) && drop.IsEntering) continue;
            eligible.Add(target);
        }
        Actor_Base chosen = null;
        if (eligible.Count > 0) {
            chosen = eligible[UnityEngine.Random.Range(0, eligible.Count)];
            chosen.Hit(Mathf.Max(1, damage * UnityEngine.Random.Range(.9f, 1.1f)), false, 0, 150, true);
            DamageHits++;
        }
        Impact?.Invoke(wolf, chosen);
    }
}

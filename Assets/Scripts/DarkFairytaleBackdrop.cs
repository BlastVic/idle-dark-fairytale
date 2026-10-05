using System.Collections.Generic;
using UnityEngine;

/// <summary>Fits the artwork to the camera and restores legacy effects when leaving battle.</summary>
public class DarkFairytaleBackdrop : MonoBehaviour
{
    Camera battleCamera;
    readonly List<Behaviour> disabledEffects = new List<Behaviour>();

    public void Initialize(Camera camera)
    {
        battleCamera = camera;
        // The old chromatic warp and saturated bloom obscure the painted artwork.
        foreach (var effect in camera.GetComponents<Behaviour>())
        {
            string type = effect.GetType().Name;
            if (effect.enabled && (type == "ChromaticAberration" || type == "BloomOptimized" || type == "PostProcessLayer"))
            {
                disabledEffects.Add(effect);
                effect.enabled = false;
            }
        }
        LateUpdate();
    }

    void LateUpdate()
    {
        if (!battleCamera) return;
        var sprite = GetComponent<SpriteRenderer>().sprite;
        var size = DarkFairytaleBattleStyle.WorldSize(battleCamera);
        float scale = Mathf.Max(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y) * 1.02f;
        transform.position = new Vector3(battleCamera.transform.position.x, battleCamera.transform.position.y, 0);
        transform.localScale = new Vector3(scale, scale, 1);
    }

    void OnDestroy()
    {
        foreach (var effect in disabledEffects)
            if (effect) effect.enabled = true;
    }
}

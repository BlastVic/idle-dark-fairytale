using UnityEngine;

// Presentation only. Keep a transient painted effect above its actor and below that actor's HP bar.
public sealed class CursedBattleParticleLayer : MonoBehaviour
{
    public bool alignToPlayer;
    void Start()
    {
        Renderer body=null;
        if(alignToPlayer){var player=FindFirstObjectByType<Player>();if(player&&player.skeletonAnimation)body=player.skeletonAnimation.GetComponent<MeshRenderer>();}
        else{
            float nearest=float.PositiveInfinity;
            foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)){
                if(!enemy.skeletonAnimation||!enemy.ab||enemy.ab.isDying)continue;
                float distance=(enemy.transform.position-transform.position).sqrMagnitude;
                if(distance<nearest){nearest=distance;body=enemy.skeletonAnimation.GetComponent<MeshRenderer>();}
            }
        }
        if(!body)return; // Standalone preview uses its authored layer.
        foreach(var renderer in GetComponentsInChildren<ParticleSystemRenderer>()){
            renderer.sortingLayerID=body.sortingLayerID;
            renderer.sortingOrder=body.sortingOrder;
            renderer.sortingFudge=-.1f;
        }
    }
}

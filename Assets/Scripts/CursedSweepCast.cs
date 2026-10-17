using UnityEngine;

// Shared authored cast effect for the actual skill and its preview.
public static class CursedSweepCast
{
    public static CommonSpineBattleEffect Spawn(Renderer body, bool preview=false)
    {
        var prefab=Resources.Load<CommonSpineBattleEffect>("Effects/DongzhuoCast");
        if(!prefab||!body)return null;
        var fx=Object.Instantiate(prefab);
        fx.preview=preview;
        Place(fx,body);
        return fx;
    }
    public static void Place(CommonSpineBattleEffect fx,Renderer body)
    {
        fx.transform.position=new Vector3(body.bounds.center.x+.8f,body.bounds.center.y,0);
        var renderer=fx.skeleton.GetComponent<Renderer>();
        renderer.sortingLayerID=body.sortingLayerID;
        renderer.sortingOrder=body.sortingOrder+1;
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>A thick check mark that stays crisp at every inventory column size.</summary>
public sealed class EquipmentSelectionCheck : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r=GetPixelAdjustedRect();
        var points=new[]{new Vector2(.08f,.47f),new Vector2(.21f,.60f),new Vector2(.40f,.40f),new Vector2(.80f,.86f),new Vector2(.94f,.73f),new Vector2(.41f,.13f)};
        foreach(var p in points)vh.AddVert(new Vector3(r.x+p.x*r.width,r.y+p.y*r.height),color,Vector2.zero);
        vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,5);vh.AddTriangle(2,3,4);vh.AddTriangle(2,4,5);
    }
}

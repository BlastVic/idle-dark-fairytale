using UnityEngine;
using UnityEngine.UI;

/// <summary>Solid arrow icon with a broad stem, independent of font glyph weight.</summary>
[AddComponentMenu("UI/Equipment Delta Arrow")]
public class EquipmentDeltaArrow : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        var points = new[] { new Vector2(.5f,1),new Vector2(0,.48f),new Vector2(1,.48f),
            new Vector2(.32f,0),new Vector2(.68f,0),new Vector2(.68f,.52f),new Vector2(.32f,.52f) };
        foreach(var p in points) vh.AddVert(new Vector3(r.x+p.x*r.width,r.y+p.y*r.height),color,Vector2.zero);
        vh.AddTriangle(0,1,2);vh.AddTriangle(3,6,5);vh.AddTriangle(3,5,4);
    }
}

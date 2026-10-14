using UnityEngine;
using UnityEngine.UI;

/// <summary>A continuous divider whose contour points toward the replacement item.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class EquipmentReplacementDivider : MaskableGraphic
{
    public float notchDepth=12, notchHalfHeight=14, centerOffset=45, thickness=1.2f;
    public Color edgeColor=new Color(.57f,.41f,.20f,.65f);
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var r=rectTransform.rect;
        float x=r.xMin+2, y=Mathf.Clamp(r.center.y-centerOffset,r.yMin+notchHalfHeight,r.yMax-notchHalfHeight);
        var points=new[]{new Vector2(x,r.yMax),new Vector2(x,y+notchHalfHeight),new Vector2(x+notchDepth,y),new Vector2(x,y-notchHalfHeight),new Vector2(x,r.yMin)};
        Stroke(mesh,points,thickness+2,edgeColor);Stroke(mesh,points,thickness,color);
    }
    static void Stroke(VertexHelper mesh,Vector2[] points,float width,Color tint)
    {
        for(int i=0;i<points.Length-1;i++){
            var a=points[i];var b=points[i+1];var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;
            int start=mesh.currentVertCount;
            mesh.AddVert(a+n,tint,Vector2.zero);mesh.AddVert(a-n,tint,Vector2.zero);mesh.AddVert(b-n,tint,Vector2.zero);mesh.AddVert(b+n,tint,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
    }
}

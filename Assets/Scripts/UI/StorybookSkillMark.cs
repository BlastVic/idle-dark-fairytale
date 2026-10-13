using UnityEngine;
using UnityEngine.UI;

/// <summary>Simple arrow/ring graphics on saved, editable UGUI nodes (no runtime hierarchy creation).</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StorybookSkillMark : MaskableGraphic
{
    public bool ring;
    [Range(1,8)] public float thickness=2.5f;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        if(!ring){vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.center.y),color,Vector2.zero);vh.AddTriangle(0,1,2);return;}
        const int count=64;
        var c=r.center;float rx=r.width*.5f,ry=r.height*.5f;
        for(int i=0;i<=count;i++){float a=i*Mathf.PI*2/count;var v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vh.AddVert(c+new Vector2(v.x*rx,v.y*ry),color,Vector2.zero);vh.AddVert(c+new Vector2(v.x*(rx-thickness),v.y*(ry-thickness)),color,Vector2.zero);if(i>0){int n=i*2;vh.AddTriangle(n-2,n-1,n);vh.AddTriangle(n,n-1,n+1);}}
    }
}

using Spine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Deforms the saved illustration graphic with one existing Spine paper mesh.</summary>
[RequireComponent(typeof(RawImage))]
public sealed class ProloguePageSurface : BaseMeshEffect
{
    Spine.Unity.SkeletonGraphic book;
    Slot slot;
    MeshAttachment paper;
    float[] vertices;
    Vector2[] uvs;
    bool following;
    public bool IsFollowing => following;
    float MeshScale => book.canvas ? book.canvas.referencePixelsPerUnit : 1f;
    public void Begin(Spine.Unity.SkeletonGraphic source, string slotName)
    {
        book=source; slot=book.Skeleton.FindSlot(slotName); paper=slot?.Attachment as MeshAttachment;
        if(paper==null)return;
        vertices=new float[paper.WorldVerticesLength];uvs=new Vector2[vertices.Length/2];
        paper.ComputeWorldVertices(slot,0,vertices.Length,vertices,0,2);
        var rt=(RectTransform)transform;var r=rt.rect;
        for(int i=0;i<uvs.Length;i++) {
            var point=rt.InverseTransformPoint(book.transform.TransformPoint(new Vector3(vertices[i*2],vertices[i*2+1],0) * MeshScale));
            uvs[i]=new Vector2((point.x-r.xMin)/r.width,(point.y-r.yMin)/r.height);
        }
        following=true;graphic.SetVerticesDirty();
    }
    public void End() { following=false;if(graphic)graphic.SetVerticesDirty(); }
    void LateUpdate(){if(following && graphic)graphic.SetVerticesDirty();}
    public override void ModifyMesh(VertexHelper vh)
    {
        if(!IsActive() || !following || paper==null)return;
        paper.ComputeWorldVertices(slot,0,vertices.Length,vertices,0,2);
        vh.Clear();
        for(int i=0;i<uvs.Length;i++) {
            var position=transform.InverseTransformPoint(book.transform.TransformPoint(new Vector3(vertices[i*2],vertices[i*2+1],0) * MeshScale));
            vh.AddVert(position,graphic.color,uvs[i]);
        }
        var triangles=paper.Triangles;
        for(int i=0;i<triangles.Length;i+=3)vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
    }
    protected override void OnDisable(){End();base.OnDisable();}
}

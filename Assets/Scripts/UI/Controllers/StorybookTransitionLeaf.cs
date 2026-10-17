using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Maps a full-screen UI capture to the story book's actual TurnPage mesh and animation.</summary>
[RequireComponent(typeof(RawImage))]
public sealed class StorybookTransitionLeaf : BaseMeshEffect
{
    public SkeletonDataAsset storyBook;
    Skeleton skeleton;
    Spine.AnimationState state;
    TrackEntry track;
    Slot slot;
    MeshAttachment paper;
    float[] vertices;
    Vector2[] sourceUV;
    Rect restBounds;
    public float Progress {get;private set;}
    public bool Following {get;private set;}
    public void Begin()
    {
        var data=storyBook.GetSkeletonData(false);
        skeleton=new Skeleton(data);state=new Spine.AnimationState(new AnimationStateData(data));
        var opened=state.SetAnimation(0,"OpenFirstPage",false);
        opened.TrackTime=opened.Animation.Duration;state.Apply(skeleton);skeleton.UpdateWorldTransform();
        state.ClearTracks();track=state.SetAnimation(0,"TurnPage",false);track.MixDuration=0;
        state.Apply(skeleton);skeleton.UpdateWorldTransform();
        slot=skeleton.FindSlot("turning_page_7");paper=(MeshAttachment)slot.Attachment;
        vertices=new float[paper.WorldVerticesLength];sourceUV=new Vector2[vertices.Length/2];
        paper.ComputeWorldVertices(slot,0,vertices.Length,vertices,0,2);
        Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
        for(int i=0;i<sourceUV.Length;i++){var p=new Vector2(vertices[i*2],vertices[i*2+1]);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        restBounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        for(int i=0;i<sourceUV.Length;i++)sourceUV[i]=Normalize(vertices[i*2],vertices[i*2+1]);
        Following=true;Sample(0);
    }
    Vector2 Normalize(float x,float y)=>new Vector2((x-restBounds.xMin)/restBounds.width,(y-restBounds.yMin)/restBounds.height);
    public void Sample(float progress){Progress=Mathf.Clamp01(progress);track.TrackTime=track.Animation.Duration*Progress;state.Apply(skeleton);skeleton.UpdateWorldTransform();graphic.SetVerticesDirty();}
    public void End(){Following=false;if(graphic)graphic.SetVerticesDirty();}
    public override void ModifyMesh(VertexHelper vh)
    {
        if(!IsActive()||!Following)return;
        paper.ComputeWorldVertices(slot,0,vertices.Length,vertices,0,2);
        var rect=((RectTransform)transform).rect;vh.Clear();
        for(int i=0;i<sourceUV.Length;i++){
            var p=Normalize(vertices[i*2],vertices[i*2+1]);
            vh.AddVert(new Vector3(rect.xMin+p.x*rect.width,rect.yMin+p.y*rect.height,0),graphic.color,sourceUV[i]);
        }
        for(int i=0;i<paper.Triangles.Length;i+=3)vh.AddTriangle(paper.Triangles[i],paper.Triangles[i+1],paper.Triangles[i+2]);
    }
    protected override void OnDisable(){End();base.OnDisable();}
}

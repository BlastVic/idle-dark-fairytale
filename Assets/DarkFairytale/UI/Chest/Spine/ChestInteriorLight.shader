Shader "UI/Chest Interior Light" {
Properties { _Mode("0 Interior, 1 Closed seam",Float)=0 }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct a {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
struct v {float4 pos:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
float _Mode;
v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.color=i.color;o.uv=i.uv;return o;}
fixed4 frag(v i):SV_Target{
    float x=(i.uv.x-.5)*340;
    float y=i.uv.y*260;
    float alpha,core;
    if(_Mode>.5){
        // The closed lid joins the body along a slightly rising seam.
        float dist=abs(y-(14+x*.055));
        float ends=1-smoothstep(136,155,abs(x));
        float lockCut=smoothstep(12,22,abs(x-7));
        core=exp(-dist*dist/2.5);
        alpha=(core+exp(-dist*dist/35)*.6)*ends*lockCut;
    }else{
        // Perspective front rim: cut all light below it, so the wooden front
        // remains opaque and the source reads as inside the chest cavity.
        float front=max(17+(x+76)*.132,17-(x+76)*.348);
        float rim=smoothstep(front,front+5,y);
        float base=exp(-pow((y-40)/16,2))*exp(-pow(x/104,4));
        float height=max(0,y-42);
        float plume=exp(-height/51)*exp(-pow(x/(80+height*.15),2));
        float beams=(pow(saturate(cos((x-height*.18)*.09)),14)*.22+pow(saturate(cos((x+height*.29)*.055)),20)*.17)*exp(-height/95);
        float upperFade=1-smoothstep(150,230,y);
        core=base;
        alpha=(base*.98+plume*.45+beams)*rim*upperFade*(1-smoothstep(126,165,abs(x)));
    }
    float3 gold=lerp(float3(1,.61,.08),float3(1,.97,.72),saturate(core));
    return fixed4(gold,saturate(alpha)*i.color.a);
}
ENDCG } } }

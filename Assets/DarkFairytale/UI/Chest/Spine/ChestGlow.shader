Shader "UI/Chest Opening Glow" {
Properties { [PerRendererData] _MainTex("UI Texture",2D)="white" {} _Color("Tint",Color)=(1,1,1,1) _Falloff("Edge falloff",Float)=2 _Rays("Radiating beams",Range(0,1))=0 }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"} Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct a {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
struct v {float4 pos:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
float _Falloff, _Rays;
v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.color=i.color;o.uv=i.uv;return o;}
fixed4 frag(v i):SV_Target{
    float2 p=(i.uv-.5)*2;
    float radius=length(p);
    float angle=atan2(p.y,p.x);
    float beams=pow(saturate(cos(angle*12+radius*1.8)),8);
    float a=pow(saturate(1-radius),max(.1,_Falloff));
    a*=lerp(1,beams*smoothstep(.12,.3,radius),_Rays);
    return fixed4(i.color.rgb,i.color.a*a);
}
ENDCG } } }

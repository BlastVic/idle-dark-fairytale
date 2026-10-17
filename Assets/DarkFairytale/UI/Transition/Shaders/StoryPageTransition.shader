Shader "DarkFairytale/UI/StoryPageTransition"
{
 Properties {
  [PerRendererData] _MainTex("Previous page",2D)="white"{}
  _PaperTex("Clean paper",2D)="white"{}
  _PaperBlend("Paper reverse",Range(0,1))=1
  _Aspect("Screen aspect",Float)=0.5625
  _Reveal("Ink reveal",Range(0,1))=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   sampler2D _MainTex,_PaperTex;float _PaperBlend,_Reveal,_Aspect;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   fixed4 frag(v2f i):SV_Target {
    float2 uv=i.uv;
    float3 outgoing=tex2D(_MainTex,uv).rgb;
    float2 paperUV=1-abs(frac(float2(uv.x*_Aspect,uv.y)*8)*2-1);
    float3 paper=tex2D(_PaperTex,paperUV).rgb;
    outgoing=lerp(outgoing,paper,_PaperBlend);
    if(_Reveal<=.001)return fixed4(outgoing,1)*i.color;
    if(_Reveal>=.999)return 0;
    // Same stationary capillary seeds / grain as the story-mode HiddenInk shader.
    float2 p=uv*float2(.66,1);
    float d=min(length(p-float2(.24,.53)),length(p-float2(.47,.30))*1.16);
    d=min(d,length(p-float2(.13,.78))*1.2);
    float threshold=d+noise(p*13)*.11+noise(p*39)*.045+noise(p*115)*.016;
    float front=_Reveal*.85-.04;
    float wet=smoothstep(threshold-.055,threshold+.055,front);
    return fixed4(outgoing,1-wet)*i.color;
   }
   ENDCG
  }
 }
}

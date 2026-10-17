Shader "DarkFairytale/Book Ember Spine" {
 Properties {
  _MainTex("Spine Atlas (Straight Alpha)",2D)="white"{}
  _Shadow("Charcoal",Color)=(0.12,0.075,0.055,1)
  _Mid("Old Gold",Color)=(0.56,0.32,0.12,1)
  _Light("Parchment Ember",Color)=(0.94,0.76,0.43,1)
  _Opacity("Opacity",Range(0,1))=0.85
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Cull Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _Shadow,_Mid,_Light;float _Opacity;
   struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct Output {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   Output vert(Input v){Output o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float4 frag(Output i):SV_Target {
    float4 t=tex2D(_MainTex,i.uv);
    float value=max(t.r,max(t.g,t.b));
    float3 ink=lerp(_Shadow.rgb,_Mid.rgb,smoothstep(0,0.68,value));
    ink=lerp(ink,_Light.rgb,smoothstep(0.68,1,value));
    // Spine vertex RGB is already premultiplied. Additive slots use vertex alpha=0.
    return float4(ink*t.a*i.color.rgb*_Opacity,t.a*i.color.a*_Opacity);
   }
   ENDCG
  }
 }
}

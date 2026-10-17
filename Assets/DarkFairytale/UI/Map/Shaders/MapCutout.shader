Shader "UI/Storybook Map Cutout"
{
 Properties {
  _DisabledUI ("Disabled greyscale", Range(0,1)) = 0
  [PerRendererData] _MainTex ("Source slice", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _Cutoff ("Paper luminance", Range(0,1)) = .55
  _Feather ("Paper antialias", Range(.001,.3)) = .12
  _InkOnly ("Flat ink", Range(0,1)) = 1
  _Mode ("Mode: ink, ring, paper, paper circle", Float) = 0
  _SourceRect ("Atlas UV rectangle", Vector) = (0,0,1,1)
  _Tiles ("Paper repeat", Vector) = (1,1,0,0)
  _Inner ("Ring hole", Float) = 0
  _StencilComp ("Stencil Comparison", Float) = 8
  _Stencil ("Stencil ID", Float) = 0
  _StencilOp ("Stencil Operation", Float) = 0
  _StencilWriteMask ("Stencil Write Mask", Float) = 255
  _StencilReadMask ("Stencil Read Mask", Float) = 255
  _ColorMask ("Color Mask", Float) = 15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
 }
 SubShader {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass { CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
  #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
  #include "UnityCG.cginc"
  #include "UnityUI.cginc"
  struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
  struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
  float _DisabledUI;
  sampler2D _MainTex; fixed4 _Color; float4 _ClipRect; float _Cutoff,_Feather,_InkOnly,_Mode,_Inner; float4 _SourceRect,_Tiles;
  v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
  fixed4 frag(v2f i):SV_Target {
  float2 local=(i.uv-_SourceRect.xy)/_SourceRect.zw;float2 uv=i.uv;
  if(_Mode>1.5&&_Mode<3.5){float2 mirrored=1-abs(frac(local*_Tiles.xy*.5)*2-1);uv=_SourceRect.xy+mirrored*_SourceRect.zw;}
  fixed4 c=tex2D(_MainTex,uv);
  if(_Mode<.5){float3 srgb=c.rgb;
  #ifndef UNITY_COLORSPACE_GAMMA
  srgb=LinearToGammaSpace(srgb);
  #endif
  float lum=dot(srgb,float3(.299,.587,.114));c.a*=1-smoothstep(_Cutoff,_Cutoff+_Feather,lum);c.rgb=lerp(c.rgb,fixed3(.0027,.0025,.0022),_InkOnly);
  }
  if((_Mode>.5&&_Mode<1.5)||(_Mode>2.5&&_Mode<3.5)){float d=length(local-.5);c.a*=1-smoothstep(.486,.497,d);if(_Inner>0)c.a*=smoothstep(_Inner-.005,_Inner+.005,d);}
  if(_Mode>3.5&&_Mode<4.5){float3 gamma=c.rgb;
  #ifndef UNITY_COLORSPACE_GAMMA
  gamma=LinearToGammaSpace(gamma);
  #endif
  float chroma=max(gamma.r,max(gamma.g,gamma.b))-min(gamma.r,min(gamma.g,gamma.b));c.a*=1-smoothstep(.15,.36,chroma);}
  if(_Mode>4.5&&_Mode<5.5){float2 p=abs(local-.5);float radius=.058;float d=length(float2(max(0,p.x-(.5-radius))/radius,p.y*2));c.a*=1-smoothstep(.96,1.015,d);}
  if(_Mode>5.5){float2 q=float2((local.x-.5)/.485,(local.y-.65)/.348);float circle=1-smoothstep(.96,1.01,length(q));float width=local.y*.72;float wedge=(1-smoothstep(width-.012,width+.012,abs(local.x-.5)))*step(local.y,.67);c.a*=max(circle,wedge);}
  c*=i.color;
  float grey=dot(c.rgb,float3(.299,.587,.114));c.rgb=lerp(c.rgb,grey.xxx,_DisabledUI);
  #ifdef UNITY_UI_CLIP_RECT
  c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
  #endif
  #ifdef UNITY_UI_ALPHACLIP
  clip(c.a-.001);
  #endif
  return c;}
  ENDCG }
 }
}

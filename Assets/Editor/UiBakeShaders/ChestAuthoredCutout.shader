// Editor-only offline extraction; never assigned to runtime materials.
Shader "Hidden/Editor/UiBake/ChestAuthoredCutout"
{
 Properties {
  _PolygonCount ("Contour points", Float) = 0
  _P0 ("Contour 0", Vector) = (0,0,0,0)
  _P1 ("Contour 1", Vector) = (0,0,0,0)
  _P2 ("Contour 2", Vector) = (0,0,0,0)
  _P3 ("Contour 3", Vector) = (0,0,0,0)
  _P4 ("Contour 4", Vector) = (0,0,0,0)
  _P5 ("Contour 5", Vector) = (0,0,0,0)
  _P6 ("Contour 6", Vector) = (0,0,0,0)
  _P7 ("Contour 7", Vector) = (0,0,0,0)
  _P8 ("Contour 8", Vector) = (0,0,0,0)
  _P9 ("Contour 9", Vector) = (0,0,0,0)
  _P10 ("Contour 10", Vector) = (0,0,0,0)
  _P11 ("Contour 11", Vector) = (0,0,0,0)
  _P12 ("Contour 12", Vector) = (0,0,0,0)
  _P13 ("Contour 13", Vector) = (0,0,0,0)
  _P14 ("Contour 14", Vector) = (0,0,0,0)
  _P15 ("Contour 15", Vector) = (0,0,0,0)
  _P16 ("Contour 16", Vector) = (0,0,0,0)
  _P17 ("Contour 17", Vector) = (0,0,0,0)
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
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend One Zero ColorMask [_ColorMask]
  Pass { CGPROGRAM
  #pragma target 3.0
  #pragma vertex vert
  #pragma fragment frag
  #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
  #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
  #include "UnityCG.cginc"
  #include "UnityUI.cginc"
  struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
  struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
  float _PolygonCount;
  float4 _P0;
  float4 _P1;
  float4 _P2;
  float4 _P3;
  float4 _P4;
  float4 _P5;
  float4 _P6;
  float4 _P7;
  float4 _P8;
  float4 _P9;
  float4 _P10;
  float4 _P11;
  float4 _P12;
  float4 _P13;
  float4 _P14;
  float4 _P15;
  float4 _P16;
  float4 _P17;
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
  if(_Mode>5.5&&_Mode<6.5){
    float4 points[18]={_P0,_P1,_P2,_P3,_P4,_P5,_P6,_P7,_P8,_P9,_P10,_P11,_P12,_P13,_P14,_P15,_P16,_P17};
    bool inside=false;float distanceToEdge=2;
    for(int index=0;index<18;index++){
      if(index<(int)_PolygonCount){
        int prev=(index+(int)_PolygonCount-1)%(int)_PolygonCount;
        float2 a=points[index].xy,b=points[prev].xy;
        float2 delta=b-a;float t=saturate(dot(local-a,delta)/max(dot(delta,delta),.0000001));
        distanceToEdge=min(distanceToEdge,length(local-(a+t*delta)));
        if((a.y>local.y)!=(b.y>local.y)){
          float crossX=(b.x-a.x)*(local.y-a.y)/(b.y-a.y)+a.x;
          if(local.x<crossX)inside=!inside;
        }
      }
    }
    c.a*=smoothstep(-.0015,.0015,inside?distanceToEdge:-distanceToEdge);
  }
  if(_Mode>6.5){
    float3 rgb=c.rgb;
    #ifndef UNITY_COLORSPACE_GAMMA
    rgb=LinearToGammaSpace(rgb);
    #endif
    float lum=dot(rgb,float3(.299,.587,.114));
    float edge=min(min(local.x,1-local.x),min(local.y,1-local.y));
    c.a*=(1-smoothstep(_Cutoff,_Cutoff+_Feather,lum))*smoothstep(0,.09,edge);
  }
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

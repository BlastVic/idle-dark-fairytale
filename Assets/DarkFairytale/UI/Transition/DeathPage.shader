Shader "UI/Storybook Death Page" {
Properties { _MainTex ("Final battle", 2D) = "white" {} _Progress ("Colour drained", Range(0,1)) = 0 _InkProgress ("Blood ink coverage", Range(0,1)) = 0 _InkTex ("Approved ink paper", 2D) = "black" {} }
SubShader { Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" } Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
sampler2D _MainTex, _InkTex;float _Progress,_InkProgress;
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
float grain(float2 p){return .55*noise(p)+.28*noise(p*2.1)+.12*noise(p*4.3)+.05*noise(p*9.2);}
v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
fixed4 frag(v2f i):SV_Target {
 fixed4 c=tex2D(_MainTex,i.uv);
 #ifndef UNITY_COLORSPACE_GAMMA
 c.rgb=GammaToLinearSpace(c.rgb);
 #endif
 float red=smoothstep(.04,.17,c.r-max(c.g,c.b));
 float ordinary=smoothstep(0,.65,_Progress);
 float cloak=smoothstep(.48,1,_Progress);
 float fade=lerp(ordinary,cloak,red);
 float lum=dot(c.rgb,float3(.299,.587,.114));
 float3 paper=lum*float3(.91,.88,.82);
 c.rgb=lerp(c.rgb,paper,fade);
 c.rgb*=lerp(1,.81,smoothstep(.65,1,_Progress));
 // A fixed fibrous contour advances down the page, with a narrow wet crimson edge.
 float2 uv=i.uv;
 float irregular=grain(float2(uv.x*8,uv.y*10));
 float fibres=noise(float2(uv.x*190,uv.y*80));
 float depth=1-uv.y + (irregular-.5)*.23 + (fibres-.5)*.018;
 float front=lerp(-.2,1.2,pow(_InkProgress,1.12));
 float ink=smoothstep(-.008,.008,front-depth);
 float wet=1-smoothstep(.005,.055,front-depth);
 // Sample only the clean opaque fourth panel of the approved storyboard.
 float3 pigment=tex2D(_InkTex,float2(.761+uv.x*.22,.15+uv.y*.70)).rgb;
 pigment=lerp(pigment*.72,float3(.17,.012,.018),wet*.7);
 // Brief dark impression immediately behind the front; fully gone before completion.
 float impression=(1-smoothstep(.025,.14,front-depth))*(1-step(.96,_InkProgress));
 pigment*=lerp(1,.55+lum*.65,impression);
 c.rgb=lerp(c.rgb,pigment,ink);
 return c*i.color;
}
ENDCG }
} }

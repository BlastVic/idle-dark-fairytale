Shader "DarkFairytale/UI/HiddenInk"
{
    Properties
    {
        [PerRendererData] _MainTex ("Home", 2D) = "white" {}
        _SourceUV ("Source UV crop", Vector) = (0,0,1,1)
        _PaperTex ("Paper surface", 2D) = "white" {}
        _PaperBlend ("Printed pigment", Range(0,1)) = 0
        _EdgeWidth ("Pigment feather width", Range(0.02,0.2)) = .06
        _Reveal ("Ink reveal", Range(0,1)) = 0
        _PaperEdge ("Paper pigment edge", Range(0,1)) = 1
        _Feather ("Wet edge softness", Range(0.01,0.15)) = 0.055
        _InkColor ("Developing ink", Color) = (0.25,0.19,0.14,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex, _PaperTex;
            float _PaperBlend, _EdgeWidth;
            float _Reveal, _Feather, _PaperEdge;
            float4 _SourceUV;
            fixed4 _InkColor;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p) {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            fixed4 frag(v2f i):SV_Target
            {
                if (any(i.uv < 0) || any(i.uv > 1)) discard;
                fixed4 source=tex2D(_MainTex,i.uv*_SourceUV.zw+_SourceUV.xy)*i.color;
                // The pigment thins into the real paper below, with stationary irregular fibers.
                float2 edgeUV=i.uv*float2(.75,1);
                float edge=min(min(edgeUV.x,.75-edgeUV.x),min(edgeUV.y,1-edgeUV.y));
                float fibers=noise(edgeUV*lerp(38,24,_PaperBlend))*lerp(.018,.04,_PaperBlend)+noise(edgeUV*113)*.007;
                float paperMask=smoothstep(.006,_EdgeWidth,edge-fibers);
                float rim=(1-paperMask)*_PaperEdge;
                source.a*=lerp(1,paperMask,_PaperEdge);
                source.rgb=lerp(source.rgb,source.rgb*float3(1.06,.97,.83),rim*.4);
                float3 paper = tex2D(_PaperTex,i.uv).rgb;
                float grain = noise(i.uv*320);
                source.rgb = lerp(source.rgb, source.rgb * paper * 1.12 + paper * .04, _PaperBlend);
                source.rgb *= lerp(1, .94 + grain*.12, _PaperBlend);
                source.a *= lerp(1,.93,_PaperBlend);
                if (_Reveal>=0.999) return source;
                if (_Reveal<=0.001) return 0;
                // Stationary paper grain: uneven capillary fronts grow from several ink seeds.
                float2 p=i.uv*float2(0.66,1);
                float d=min(length(p-float2(.24,.53)), length(p-float2(.47,.30))*1.16);
                d=min(d,length(p-float2(.13,.78))*1.2);
                float revealGrain=noise(p*13)*.11+noise(p*39)*.045+noise(p*115)*.016;
                float threshold=d+revealGrain;
                float front=_Reveal*.85-.04;
                float wet=smoothstep(threshold-_Feather,threshold+_Feather,front);
                float developed=smoothstep(threshold+.02,threshold+.19,front);
                float luma=dot(source.rgb,float3(.299,.587,.114));
                // Dark details emerge first, then pigment develops into the original full-color UI.
                float detail=lerp(.35,1,1-luma);
                source.rgb=lerp(_InkColor.rgb*(.55+.65*luma),source.rgb,developed);
                source.a*=wet*lerp(detail,1,developed);
                return source;
            }
            ENDCG
        }
    }
}

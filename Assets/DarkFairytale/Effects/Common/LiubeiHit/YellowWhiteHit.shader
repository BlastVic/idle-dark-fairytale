Shader "DarkFairytale/Yellow White Hit" {
	Properties {
        _WhiteOpacity ("Peripheral White Opacity", Range(0,1)) = 0.15
        _YellowOpacity ("Peripheral Yellow Opacity", Range(0,1)) = 0.45
		_Cutoff ("Shadow alpha cutoff", Range(0,1)) = 0.1
		[NoScaleOffset] _MainTex ("Main Texture", 2D) = "black" {}
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		[HideInInspector] _StencilRef("Stencil Reference", Float) = 1.0
		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8 // Set to Always as default

		// Outline properties are drawn via custom editor.
		[HideInInspector] _OutlineWidth("Outline Width", Range(0,8)) = 3.0
		[HideInInspector] _OutlineColor("Outline Color", Color) = (1,1,0,1)
		[HideInInspector] _OutlineReferenceTexWidth("Reference Texture Width", Int) = 1024
		[HideInInspector] _ThresholdEnd("Outline Threshold", Range(0,1)) = 0.25
		[HideInInspector] _OutlineSmoothness("Outline Smoothness", Range(0,1)) = 1.0
		[HideInInspector][MaterialToggle(_USE8NEIGHBOURHOOD_ON)] _Use8Neighbourhood("Sample 8 Neighbours", Float) = 1
		[HideInInspector] _OutlineMipLevel("Outline Mip Level", Range(0,3)) = 0
	}

	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

		Fog { Mode Off }
		Cull Off
		ZWrite Off
		Blend One OneMinusSrcAlpha
		Lighting Off

		Stencil {
			Ref[_StencilRef]
			Comp[_StencilComp]
			Pass Keep
		}

		Pass {
			Name "Normal"

			CGPROGRAM
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			sampler2D _MainTex;
            float _WhiteOpacity, _YellowOpacity;

			struct VertexInput {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 vertexColor : COLOR;
			};

			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 vertexColor : COLOR;
			};

			VertexOutput vert (VertexInput v) {
				VertexOutput o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.vertexColor = v.vertexColor;
				return o;
			}

			float4 frag (VertexOutput i) : SV_Target {
				float4 texColor = tex2D(_MainTex, i.uv);
                // Exclude every bd2 frame: the central slash keeps its exact source RGB.
                bool centralSlash=(i.uv.x>0.245002701 && i.uv.y>0.007317073 && i.uv.x<0.263371151 && i.uv.y<0.860975610) || (i.uv.x>0.295245813 && i.uv.y>0.856097561 && i.uv.x<0.423284711 && i.uv.y<0.992682927) || (i.uv.x>0.423824959 && i.uv.y>0.836585366 && i.uv.x<0.572393301 && i.uv.y<0.929268293) || (i.uv.x>0.858725014 && i.uv.y>0.548780488 && i.uv.x<0.993246894 && i.uv.y<0.636585366) || (i.uv.x>0.423824959 && i.uv.y>0.934146341 && i.uv.x<0.559427337 && i.uv.y<0.987804878) || (i.uv.x>0.000810373 && i.uv.y>0.943902439 && i.uv.x<0.133711507 && i.uv.y<0.978048780);
                if(!centralSlash){
                    float peak=max(texColor.r,max(texColor.g,texColor.b));
                    float saturation=(peak-min(texColor.r,min(texColor.g,texColor.b)))/max(peak,0.0001);
                    float redTail=saturate((texColor.r-texColor.g)/max(texColor.r,0.0001));
                    float whiteTail=smoothstep(0.15,0.85,redTail);
                    bool burstShape=(i.uv.x>0.379524581 && i.uv.y>0.007317073 && i.uv.x<0.447055646 && i.uv.y<0.831707317) || (i.uv.x>0.263911399 && i.uv.y>0.007317073 && i.uv.x<0.378984333 && i.uv.y<0.851219512) || (i.uv.x>0.124527283 && i.uv.y>0.007317073 && i.uv.x<0.244462453 && i.uv.y<0.914634146) || (i.uv.x>0.000810373 && i.uv.y>0.007317073 && i.uv.x<0.123987034 && i.uv.y<0.939024390) || (i.uv.x>0.447595894 && i.uv.y>0.007317073 && i.uv.x<0.571853052 && i.uv.y<0.778048780) || (i.uv.x>0.966234468 && i.uv.y>0.231707317 && i.uv.x<0.985683414 && i.uv.y<0.495121951) || (i.uv.x>0.902485143 && i.uv.y>0.007317073 && i.uv.x<0.934900054 && i.uv.y<0.514634146) || (i.uv.x>0.670178282 && i.uv.y>0.007317073 && i.uv.x<0.705294435 && i.uv.y<0.587804878) || (i.uv.x>0.572933549 && i.uv.y>0.602439024 && i.uv.x<0.635062129 && i.uv.y<0.929268293) || (i.uv.x>0.858725014 && i.uv.y>0.641463415 && i.uv.x<0.912749865 && i.uv.y<0.982926829) || (i.uv.x>0.763101026 && i.uv.y>0.558536585 && i.uv.x<0.800918422 && i.uv.y<0.987804878) || (i.uv.x>0.725823879 && i.uv.y>0.573170732 && i.uv.x<0.762560778 && i.uv.y<0.943902439) || (i.uv.x>0.964613722 && i.uv.y>0.641463415 && i.uv.x<0.999189627 && i.uv.y<0.909756098) || (i.uv.x>0.945705024 && i.uv.y>0.231707317 && i.uv.x<0.965694219 && i.uv.y<0.504878049);
                    float3 yellowWhite=lerp(float3(1,0.92,0.06),float3(1,1,1),whiteTail);
                    float whiteness=1-saturation*(1-whiteTail);
                    texColor.a*=lerp(_YellowOpacity,_WhiteOpacity,whiteness);
                    texColor.rgb=lerp(float3(1,1,1),yellowWhite,saturation)*(burstShape?max(peak,0.85):peak);
                }


				#if defined(_STRAIGHT_ALPHA_INPUT)
				texColor.rgb *= texColor.a;
				#endif

				return (texColor * i.vertexColor);
			}
			ENDCG
		}

		Pass {
			Name "Caster"
			Tags { "LightMode"="ShadowCaster" }
			Offset 1, 1
			ZWrite On
			ZTest LEqual

			Fog { Mode Off }
			Cull Off
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			fixed _Cutoff;

			struct VertexOutput {
				V2F_SHADOW_CASTER;
				float4 uvAndAlpha : TEXCOORD1;
			};

			VertexOutput vert (appdata_base v, float4 vertexColor : COLOR) {
				VertexOutput o;
				o.uvAndAlpha = v.texcoord;
				o.uvAndAlpha.a = vertexColor.a;
				TRANSFER_SHADOW_CASTER(o)
				return o;
			}

			float4 frag (VertexOutput i) : SV_Target {
				fixed4 texcol = tex2D(_MainTex, i.uvAndAlpha.xy);
				clip(texcol.a * i.uvAndAlpha.a - _Cutoff);
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
	CustomEditor "SpineShaderWithOutlineGUI"
}

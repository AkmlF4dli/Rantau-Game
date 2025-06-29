Shader "Custom/MenuBlur"
{
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
        _SamplingDistance("Sampling Distance", Range(0.5, 3)) = 1.5
    }
    SubShader
    {
        Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		#define E 2.71828f

		sampler2D _MainTex;

		CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_TexelSize;
			//uint _GridSize;
			float _Spread;
            float _SamplingDistance;
		CBUFFER_END


        static const int samplingCount = 7;
        static const half weights[samplingCount] = { 0.0205, 0.0855, 0.232, 0.324, 0.232, 0.0855, 0.0205 };

		struct appdata
		{
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		v2f vert(appdata v)
		{
			v2f o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.uv = v.uv;
			return o;
		}

		ENDHLSL

        Pass
        {
			Name "Horizontal"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_horizontal

            float4 frag_horizontal (v2f i) : SV_Target
			{
				float3 col = float3(0.0f, 0.0f, 0.0f);
				float gridSum = 0.0f;

                [unroll]
                for (int j = -3; j <= 3; j++)
				{
					float2 uv = i.uv + float2(_MainTex_TexelSize.x * j * _SamplingDistance, 0.0f);
                    col += tex2D(_MainTex, uv).xyz * weights[j + 3];
				}

                return float4(col, 1.0f);
			}
            ENDHLSL
        }

		Pass
        {
			Name "Vertical"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_vertical

            float4 frag_vertical (v2f i) : SV_Target
			{
				float3 col = float3(0.0f, 0.0f, 0.0f);
				float gridSum = 0.0f;

                [unroll]
                for (int j = -3; j <= 3; j++)
				{
					float2 uv = i.uv + float2(0.0f, _MainTex_TexelSize.y * j * _SamplingDistance);
                    col += tex2D(_MainTex, uv).xyz * weights[j + 3];
				}

				return float4(col, 1.0f);
			}
            ENDHLSL
        }
    }
}
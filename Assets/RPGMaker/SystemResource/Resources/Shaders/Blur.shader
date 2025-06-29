Shader "Hidden/Blur"
{
    // feat by https://edom18.hateblo.jp/entry/2018/12/30/171214
    // feat by https://elekibear.com/post/20230127_01#google_vignette
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Tags
        {
            "RenderType" = "Opaque"
        }

        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        sampler2D _MainTex;
        half4 _OffsetX;
        half4 _OffsetY;
        static const int samplingCount = 10;
        half _Weights[samplingCount];
        float _RadialBlur;

        fixed4 frag0(v2f i) : SV_Target
        {
            half4 col = 0;

            [unroll]
            for (int j = samplingCount - 1; j > 0; j--)
            {
                col += tex2D(_MainTex, i.uv - _OffsetX.xy * j) * _Weights[j];
            }

            [unroll]
            for (int j = 0; j < samplingCount; j++)
            {
                col += tex2D(_MainTex, i.uv + _OffsetY.xy * j) * _Weights[j];
            }

            return col;
        }

        fixed4 frag1(v2f i) : SV_Target
        {
            fixed4 col = 0;
            const half2 symmetryUv = i.uv - 0.5;
            const half distance = length(symmetryUv);
            for (int j = 0; j < samplingCount; j++)
            {
                const float uvOffset = 1 - _RadialBlur * j / samplingCount * distance;
                col += tex2D(_MainTex, symmetryUv * uvOffset + 0.5);
            }
            col /= samplingCount;
            return col;
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag0
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag1
            ENDCG
        }
    }
}
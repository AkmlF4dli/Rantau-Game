Shader "Hidden/WipeCircle"
{
    // feat by https://soramamenatan.hatenablog.com/entry/2019/12/08/203220
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest" "RenderType"="TransparentCutout"
        }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
            float _Transition;

            // 内積
            float circle(float2 p)
            {
                return dot(p, p);
            }

            // 範囲変換
            float map(float value, float min1, float max1, float min2, float max2)
            {
                return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 円の大きさと位置の調整
                float size = map(_Transition, 0.0, 1.0, 0.0001, 0.2);
                float2 f_st = frac(i.uv) * 1.0 - 0.5;
                // 画面解像度に影響されないようにする
                f_st.y *= _ScreenParams.y / _ScreenParams.x;
                float ci = circle(f_st);
                // ciが0.1より大きいならクリップ
                if (ci > size)
                {
                    return float4(0, 0, 0, 0);
                }
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
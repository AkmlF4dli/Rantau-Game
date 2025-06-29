Shader "Hidden/Fill"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Tags
        {
            "RenderType"="Transparent" "Queue" = "Transparent"
        }

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
            float4 _Color1;
            float4 _Color2;
            float _Horizontal;
            float _Vertical;
            float _Inverse;
            float _Blend;
            float _MixRatio;
            
            fixed4 frag(v2f i) : SV_Target
            {
                // NOTE: 強度を塗る色のアルファとしている為、通常のグラフィックソフト等とは異なる
                
                fixed4 src = tex2D(_MainTex, i.uv);
                fixed4 dst = lerp(_Color1, _Color2, (_Inverse + i.uv.x * (1 - _Inverse * 2) * _Horizontal + (1 - i.uv.y) * _Vertical) / (_Horizontal + _Vertical));
                half3 src_remain = src.rgb * (1 - dst.a);
                
                half3 normal = dst.rgb * dst.a;
                half4 normal_final = half4(normal + src_remain, src.a);
                
                half3 mul = src.rgb * (dst.rgb * dst.a);
                half4 mul_final = half4(mul.rgb + src_remain, src.a);
                
                half3 add = src.rgb + (dst.rgb * dst.a);
                half4 add_final = half4(add, src.a);
                
                half div_r = step(dst.r, 0.004) * 0.004 + (1 - step(dst.r, 0.004)) * dst.r;
                half div_g = step(dst.g, 0.004) * 0.004 + (1 - step(dst.g, 0.004)) * dst.g;
                half div_b = step(dst.b, 0.004) * 0.004 + (1 - step(dst.b, 0.004)) * dst.b;
                half div_a = step(dst.a, 0.004) * 0.004 + (1 - step(dst.a, 0.004)) * dst.a;
                half3 div_dst = half3(div_r, div_g, div_b);
                half3 div = src.rgb / (1 - (1 - div_dst.rgb) * div_a);
                half4 div_final = half4(div, src.a);
                
                half3 screen = (1 - src.rgb) * (1 - dst.rgb * dst.a);
                half4 screen_final = half4(1 - screen, src.a);

                half3 overlay_mul = mul * (1 + dst.a) + src_remain;
                half3 overlay_screen = 1 - screen * (1 + dst.a);
                half3 overlay = step(src, 0.5) * overlay_mul + (1 - step(src, 0.5)) * overlay_screen;
                half4 overlay_final = half4(overlay, src.a);

                half4 color
                    = normal_final * step(0, _Blend) * step(_Blend, 0)
                    + mul_final * step(1, _Blend) * step(_Blend, 1)
                    + add_final * step(2, _Blend) * step(_Blend, 2)
                    + div_final * step(3, _Blend) * step(_Blend, 3)
                    + screen_final * step(4, _Blend) * step(_Blend, 4)
                    + overlay_final * step(5, _Blend) * step(_Blend, 5);

                color = clamp(color, 0, 1);

                half4 final = half4(src.rgb * (1 - _MixRatio) + color.rgb * _MixRatio, 1);

                return final;
            }
            ENDCG
        }
    }
}
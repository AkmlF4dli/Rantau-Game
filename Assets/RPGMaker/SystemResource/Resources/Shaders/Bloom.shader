Shader "Hidden/Bloom"
{
    // feat by https://shibuya24.info/entry/unity-shader-bloom
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
        sampler2D _Temp;
        float _Strength;
        float _Blur;

        fixed4 frag0(v2f_img i) : SV_Target
        {
            fixed4 col = tex2D(_MainTex, i.uv);
            float bright = (col.r + col.g + col.b) / 3;
            float tmp = step(.3f, bright);
            return tex2D(_MainTex, i.uv) * tmp * _Strength;
        }

        fixed4 frag1(v2f_img i) : SV_Target
        {
            float u = 1 / _ScreenParams.x;
            float v = 1 / _ScreenParams.y;

            fixed4 result;
            for (float x = 0; x < _Blur; x++)
            {
                float xx = i.uv.x + (x - _Blur / 2) * u;

                for (float y = 0; y < _Blur; y++)
                {
                    float yy = i.uv.y + (y - _Blur / 2) * v;
                    fixed4 smp = tex2D(_Temp, float2(xx, yy));
                    result += smp;
                }
            }

            result /= _Blur * _Blur;
            return tex2D(_MainTex, i.uv) + result;
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag0
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag1
            ENDCG
        }
    }
}
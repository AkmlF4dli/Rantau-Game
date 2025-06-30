Shader "Hidden/Glitch"
{
    // feat by https://zenn.dev/umeyan/articles/e312dd0bd8a61f
    // feat by https://zenn.dev/kento_o/articles/d98b704e588e27
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

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

            sampler2D _MainTex;
            float _GlitchIntensity;
            float _BlockScale;
            float _NoiseSpeed;
            float _ChromaticAberration;
            float _WaveWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float random(float2 seeds)
            {
                return frac(sin(dot(seeds, float2(12.9898, 78.233))) * 43758.5453);
            }

            float blockNoise(float2 seeds)
            {
                return random(floor(seeds));
            }

            float noiserandom(float2 seeds)
            {
                return -1.0 + 2.0 * blockNoise(seeds);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 color;
                float2 gv = i.uv;
                float waveVertical = 32.0f;

                float noise = blockNoise(i.uv.y * _BlockScale);
                float2 randomvalue = noiserandom(float2(i.uv.y, _Time.y * _NoiseSpeed));
                gv.x += randomvalue * sin(sin(_GlitchIntensity) * .5) * sin(-sin(noise) * .2) * frac(_Time.y);

                float2 distortion = float2(sin(i.uv.y * waveVertical + _Time.w) * 0.1f * _WaveWidth, 0);
                distortion *= _GlitchIntensity;
                gv += distortion;

                color.r = tex2D(_MainTex, gv + float2(0.006, 0) * _ChromaticAberration * _GlitchIntensity).r;
                color.g = tex2D(_MainTex, gv).g;
                color.b = tex2D(_MainTex, gv - float2(0.008, 0) * _ChromaticAberration * _GlitchIntensity).b;
                color.a = 1.0;

                return color;
            }
            ENDCG
        }
    }
}
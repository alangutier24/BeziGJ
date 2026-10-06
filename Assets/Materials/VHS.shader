Shader "Custom/VHS"
{
    Properties
    {
        _Wobble ("Line Wobble", Range(0, 0.01)) = 0.002
        _BandStrength ("Tracking Band Strength", Range(0, 0.05)) = 0.02
        _BandSpeed ("Tracking Band Speed", Float) = 0.15
        _ChromaBlur ("Chroma Blur", Range(0, 0.02)) = 0.006
        _ChromaShift ("Chroma Shift", Range(0, 0.01)) = 0.003
        _Saturation ("Saturation", Range(0, 1.5)) = 0.8
        _Scanlines ("Scanlines", Range(0, 1)) = 0.15
        _Noise ("Noise", Range(0, 0.3)) = 0.06
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "VHS"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Wobble;
            float _BandStrength;
            float _BandSpeed;
            float _ChromaBlur;
            float _ChromaShift;
            float _Saturation;
            float _Scanlines;
            float _Noise;

            static const float3x3 RGB2YIQ = float3x3(
                0.299,  0.587,  0.114,
                0.596, -0.274, -0.322,
                0.211, -0.523,  0.312);

            static const float3x3 YIQ2RGB = float3x3(
                1.0,  0.956,  0.621,
                1.0, -0.272, -0.647,
                1.0, -1.106,  1.703);

            float Hash12(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float3 SampleRGB(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            half4 Frag (Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float t = _Time.y;
                float row = floor(uv.y * _ScreenParams.y);

                // 2. Temblor por línea (aleatorio por fila + onda lenta)
                uv.x += (Hash12(float2(row, floor(t * 30.0))) - 0.5) * _Wobble;
                uv.x += sin(uv.y * 30.0 + t * 2.0) * _Wobble * 0.5;

                // 3. Banda de tracking que recorre la pantalla
                float bandPos = 1.0 - frac(t * _BandSpeed);
                float band = 1.0 - smoothstep(0.0, 0.05, abs(uv.y - bandPos));
                uv.x += band * (Hash12(float2(row, t)) - 0.5) * _BandStrength;

                // 4. Head switching (parte inferior)
                float hs = 1.0 - smoothstep(0.0, 0.04, uv.y);
                uv.x += hs * hs * Hash12(float2(row, floor(t * 30.0))) * 0.03;

                // 1. Brillo nítido, color desenfocado y desplazado
                float y = mul(RGB2YIQ, SampleRGB(uv)).x;
                float2 iq = 0;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float o = (i / 7.0 - 0.5) * _ChromaBlur + _ChromaShift;
                    iq += mul(RGB2YIQ, SampleRGB(uv + float2(o, 0))).yz;
                }
                iq /= 8.0;
                float3 col = mul(YIQ2RGB, float3(y, iq * _Saturation));

                // 5. Scanlines
                col *= 1.0 - _Scanlines * (0.5 + 0.5 * sin(uv.y * _ScreenParams.y * PI));

                // 5. Ruido general + estática extra en la banda
                float n = Hash12(input.texcoord * _ScreenParams.xy + frac(t) * 100.0);
                col += (n - 0.5) * _Noise;
                col += band * n * 0.3;

                // 5. Negros levantados (look lavado)
                col = col * 0.9 + 0.05;

                return half4(saturate(col), 1);
            }
            ENDHLSL
        }
    }
}
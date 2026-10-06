Shader "Custom/Noir"
{
    Properties
    {
        _Blend ("Blend", Range(0, 1)) = 1
        _ContrastLow ("Contrast Low", Range(0, 1)) = 0.2
        _ContrastHigh ("Contrast High", Range(0, 1)) = 0.8
        _KeepColor ("Keep Color", Color) = (1, 0, 0, 1)
        _HueTolerance ("Hue Tolerance", Range(0, 0.5)) = 0.05
        _VignetteRadius ("Vignette Radius", Range(0, 1)) = 0.4
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1)) = 0.4
        _Grain ("Grain", Range(0, 0.3)) = 0.08
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "Noir"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Blend;
            float _ContrastLow;
            float _ContrastHigh;
            float4 _KeepColor;
            float _HueTolerance;
            float _VignetteRadius;
            float _VignetteSoftness;
            float _Grain;

            float Hash12(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 Frag (Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // 1. Luminancia
                float lum = dot(col, float3(0.2126, 0.7152, 0.0722));

                // 2. Contraste
                lum = smoothstep(_ContrastLow, _ContrastHigh, lum);

                // 3. Color selectivo
                float3 hsv = RgbToHsv(col);
                float targetHue = RgbToHsv(_KeepColor.rgb).x;
                float d = abs(hsv.x - targetHue);
                d = min(d, 1.0 - d);                       // el hue es circular: 0.98 está cerca de 0.02
                float mask = 1.0 - smoothstep(_HueTolerance, _HueTolerance + 0.05, d);
                mask *= smoothstep(0.25, 0.45, hsv.y);     // ignora píxeles poco saturados
                float3 noir = lerp(lum.xxx, col, mask);

                // 4. Viñeta
                float dist = length(uv - 0.5);
                noir *= 1.0 - smoothstep(_VignetteRadius, _VignetteRadius + _VignetteSoftness, dist);

                // 5. Grano
                float n = Hash12(uv * _ScreenParams.xy + frac(_Time.y) * 100.0);
                noir += (n - 0.5) * _Grain;

                return half4(lerp(col, noir, _Blend), 1);
            }
            ENDHLSL
        }
    }
}
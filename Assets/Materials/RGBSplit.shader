Shader "Custom/RGBSplit"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 0.5)) = 0.5
        _Speed ("Speed", Float) = 1
        _Chance ("_Chance", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags {"RenderPipeline" = "UniversalPipeline" }

        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RGBSplit"
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _Speed = 19;
            float _Chance = 0.3;

            float Hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453);
            }

            half4 Frag (Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float tick   = floor(_Time.y * _Speed);
                float active = step(1.0 - _Chance, Hash11(tick));            // 1 = este tick glitchea
                float amount = (Hash11(tick + 13.7) * 2.0 - 1.0) * _Intensity * active; // -I..+I

                float2 offset = float2(amount, 0);

                half r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).r;
                half g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).g;
                half b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset).b;

                return half4(r, g, b, 1);
            }
            
            ENDHLSL
        }
    
    }
}

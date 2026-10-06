Shader "Custom/VisionReveal"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _Color ("Color", Color) = (1,1,1,1)
        _EdgeSoftness ("Edge Softness", Float) = 0.5
        [Toggle(_INVERT)] _Invert ("Hide Inside Vision", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _INVERT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;      // SpriteRenderer.color arrives here
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4  color      : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float  _EdgeSoftness;
            CBUFFER_END

            // Set globally by DeityVision
            float3 _VisionCenter;
            float  _VisionRadius;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // Cylindrical (XZ) distance so walls reveal fully top to bottom
                float dist = length(IN.positionWS.xz - _VisionCenter.xz);
                float mask = 1.0 - smoothstep(_VisionRadius - _EdgeSoftness, _VisionRadius, dist);

                #if _INVERT
                    mask = 1.0 - mask;
                #endif

                col.a *= mask;
                clip(col.a - 0.001);
                return col;
            }
            ENDHLSL
        }
    }
}

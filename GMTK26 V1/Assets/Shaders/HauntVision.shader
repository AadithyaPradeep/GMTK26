Shader "GMTK/HauntVision"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Center ("Center", Vector) = (0, 0, 0, 0)
        _Radius ("Radius", Float) = 3
        _Softness ("Softness", Float) = 0.45
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Center;
                float _Radius;
                float _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 delta = input.positionWS.xy - _Center.xy;
                float dist = length(delta);
                float soft = max(0.01, _Softness);
                float alpha = smoothstep(_Radius, _Radius + soft, dist);
                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }

    // Built-in fallback for editor / non-URP peek
    FallBack Off
}

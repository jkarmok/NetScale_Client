Shader "Custom/FogOfWarOptimized"
{
    Properties
    {
        _FogTex ("Fog Texture", 2D) = "white" {}
        _MapSize ("Map Size", Vector) = (100, 100, 0, 0)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest Always  // ← ДОБАВЬТЕ ЭТУ СТРОКУ!

        Pass
        {
            Name "FogOfWarPassOptimized"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 fogUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FogTex_ST;
                float4 _MapSize;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = vertexInput.positionCS;
                
                // Вычисление UV в вершинном шейдере для производительности
                float3 worldPos = vertexInput.positionWS;
                output.fogUV.x = (worldPos.x + _MapSize.x * 0.5) / _MapSize.x;
                output.fogUV.y = (worldPos.z + _MapSize.y * 0.5) / _MapSize.y;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Простое сэмплирование без дополнительных вычислений
                return SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, input.fogUV);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
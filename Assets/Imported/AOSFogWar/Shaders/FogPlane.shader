//
// Created :    Spring 2023
// Author :     SeungGeon Kim (keithrek@hanmail.net)
// Project :    FogWar
// Filename :   FogPlane.shader (cg shader)
// 
// All Content (C) 2022 Unlimited Fischl Works, all rights reserved.
//

// Упрощенная версия для лучшей производительности

Shader "FogWar/FogPlane"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _BlurOffset("BlurOffset", Range(0, 10)) = 1
        [Toggle(ENABLE_BLUR)] _EnableBlur("Enable Gaussian Blur", Float) = 1
    }

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

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    float4 _Color;
    float _BlurOffset;
    float _EnableBlur;

    v2f vert(appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        return o;
    }

    fixed4 frag_no_blur(v2f i) : SV_Target
    {
        // Простая выборка без blur
        fixed4 col = tex2D(_MainTex, i.uv);
        return col * _Color;
    }

    fixed4 frag_blur(v2f i) : SV_Target
    {
        // Gaussian blur только если включен
        #if ENABLE_BLUR
            float offset = _BlurOffset * _MainTex_TexelSize;

            half GaussianKernel[9] =
            {
                1,2,1,
                2,4,2,
                1,2,1
            };

            fixed4 col = fixed4(0,0,0,0);

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    col +=
                    tex2D(_MainTex, i.uv + fixed2(x - 1, y - 1) * offset) *
                    GaussianKernel[x * 1 + y * 3];
                }
            }

            col /= 16;
        #else
            fixed4 col = tex2D(_MainTex, i.uv);
        #endif

        return col * _Color;
    }

    ENDCG

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        CULL BACK
        ZWrite OFF
        ZTest Always
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blur
            #pragma multi_compile __ ENABLE_BLUR
            ENDCG
        }
    }
    
    // Fallback для устройств без шейдерной поддержки
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        CULL BACK
        ZWrite OFF
        ZTest Always
        LOD 50

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_no_blur
            ENDCG
        }
    }
}
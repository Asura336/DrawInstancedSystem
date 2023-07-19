Shader "BRP/Instanced/Opaqueprocedural"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Toggle(_METALLICGLOSSMAP)] _UseMetallicGlossMap("_UseMetallicGlossMap", Int) = 0
        [NoScaleOffset]_MetallicGlossMap ("MetallicRoughness", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [Toggle(_NORMALMAP)] _UseBumpMap("_UseBumpMap", Int) = 0
        [NoScaleOffset]_BumpMap ("NormalMap", 2D) = "bump" {}
        _BumpScale("Normal",Range(0,10)) = 1

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Cull [_Cull]

        CGPROGRAM
        #pragma target 4.5
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma multi_compile_instancing
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation

        #pragma shader_feature _METALLICGLOSSMAP
        #pragma shader_feature _NORMALMAP

        #include "UnityShaderVariables.cginc"

        #if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_SWITCH) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC)))
        #define SUPPORT_STRUCTUREDBUFFER
        #endif

        #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && defined(SUPPORT_STRUCTUREDBUFFER)
        #define ENABLE_INSTANCING
        #endif

        #include "DrawProceduralGPU.hlsl"
        #include "DrawFunctions.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}

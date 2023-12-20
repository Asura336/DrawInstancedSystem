sampler2D _MainTex;
sampler2D _BumpMap;

sampler2D _MetallicGlossMap;

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 texcoord : TEXCOORD;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Input
{
    float2 uv_MainTex;
};

// DrawMeshInstancedProcedural
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<half4> _Colors;
    #define _COLOR _Colors[unity_InstanceID]
#else
    half4 _Color;
    #define _COLOR _Color
#endif


half _BumpScale;
half _Glossiness;
half _Metallic;

half3 TexNormal(float2 uv)
{
    half3 normal = UnpackNormal(tex2D(_BumpMap,uv));
    normal.xy *= _BumpScale;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy,normal.xy)));
    return normal;
}

half2 TexMetallicGloss(float2 uv)
{
    half2 mg = half2(_Metallic,_Glossiness);
    mg *= tex2D(_MetallicGlossMap, uv).ra;
    return mg;
}

void surf (Input IN, inout SurfaceOutputStandard o)
{
    // Albedo comes from a texture tinted by color
    half4 c = tex2D (_MainTex, IN.uv_MainTex) * _COLOR;
    o.Albedo = c.rgb;
    o.Alpha = c.a;

    #if defined(_METALLICGLOSSMAP)
        // Metallic and smoothness come from slider variables
        half2 mr = TexMetallicGloss(IN.uv_MainTex);
        o.Metallic = mr.x;
        o.Smoothness = mr.y;
    #endif

    #if defined(_NORMALMAP)
        o.Normal = TexNormal(IN.uv_MainTex);
    #endif
}
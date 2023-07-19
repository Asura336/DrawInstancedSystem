// DrawMeshInstancedProcedural
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    //StructuredBuffer<float3x4> _Matrices;
    StructuredBuffer<float4x4> _Matrices;
#endif

// https://answers.unity.com/questions/218333/shader-inversefloat4x4-function.html
inline float4x4 inverse(in float4x4 input)
{
#define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
    //determinant(float3x3(input._22_23_23, input._32_33_34, input._42_43_44))
    
    float4x4 cofactors = float4x4(
    minor(_22_23_24, _32_33_34, _42_43_44),
    -minor(_21_23_24, _31_33_34, _41_43_44),
    minor(_21_22_24, _31_32_34, _41_42_44),
    -minor(_21_22_23, _31_32_33, _41_42_43),
    
    -minor(_12_13_14, _32_33_34, _42_43_44),
    minor(_11_13_14, _31_33_34, _41_43_44),
    -minor(_11_12_14, _31_32_34, _41_42_44),
    minor(_11_12_13, _31_32_33, _41_42_43),
    
    minor(_12_13_14, _22_23_24, _42_43_44),
    -minor(_11_13_14, _21_23_24, _41_43_44),
    minor(_11_12_14, _21_22_24, _41_42_44),
    -minor(_11_12_13, _21_22_23, _41_42_43),
    
    -minor(_12_13_14, _22_23_24, _32_33_34),
    minor(_11_13_14, _21_23_24, _31_33_34),
    -minor(_11_12_14, _21_22_24, _31_32_34),
    minor(_11_12_13, _21_22_23, _31_32_33)
    );
#undef minor
    return transpose(cofactors) / determinant(input);
}

void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        unity_ObjectToWorld = _Matrices[unity_InstanceID];
        unity_WorldToObject = inverse(_Matrices[unity_InstanceID]);
        // float3x4 m = _Matrices[unity_InstanceID];
        // unity_ObjectToWorld._m00_m01_m02_m03 = m._m00_m01_m02_m03;
        // unity_ObjectToWorld._m10_m11_m12_m13 = m._m10_m11_m12_m13;
        // unity_ObjectToWorld._m20_m21_m22_m23 = m._m20_m21_m22_m23;
        // unity_ObjectToWorld._m30_m31_m32_m33 = float4(0, 0, 0, 1);
#endif
}
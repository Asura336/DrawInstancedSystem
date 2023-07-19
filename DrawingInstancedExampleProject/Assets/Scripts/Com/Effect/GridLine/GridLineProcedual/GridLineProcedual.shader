Shader "Unlit/GridLineProcedual"
{
    Properties
    {
        _Color ("main color", Color) = (1, 1, 1, 1)
        _FadeColor("color when fade", COLOR) = (1, 1, 1, 1)
        _Clip ("clip when far than", float) = 1000
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "Queue"="Transparent"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
            #include "UnityCG.cginc"
            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float depth : DEPTH;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _Color;
            float4 _FadeColor;
            float _Clip;

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            StructuredBuffer<float4x4> _Matrices;
            #endif

            void ConfigureProcedural() {
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                unity_ObjectToWorld = _Matrices[unity_InstanceID];
                #endif
            }

            v2f vert (appdata v, uint id : SV_INSTANCEID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = ComputeScreenPos(o.vertex).y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                float clipDis = _Clip;
                float alpha = saturate((clipDis - i.depth) / clipDis);
                fixed4 col = lerp(_FadeColor, _Color, alpha);
                col.a *= alpha;
                clip(alpha - 0.0001);

                return col;
            }
            ENDCG
        }
    }
}

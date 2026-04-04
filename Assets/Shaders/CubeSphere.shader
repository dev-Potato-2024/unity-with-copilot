Shader "Custom/CubeSphere"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Radius ("Radius", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Radius;
            int _Resolution;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
            };

            float3 FaceCubePos(int face, float u, float v)
            {
                if (face == 0) return float3(1, v, u);
                if (face == 1) return float3(-1, v, -u);
                if (face == 2) return float3(u, 1, v);
                if (face == 3) return float3(u, -1, -v);
                if (face == 4) return float3(u, v, 1);
                return float3(-u, v, -1);
            }

            v2f vert(uint id : SV_VertexID)
            {
                v2f o;

                int res = max(_Resolution, 2);
                int quadsPerFace = (res - 1) * (res - 1);
                int trianglePerFace = quadsPerFace * 2;

                int tri = id / 3;
                int face = tri / trianglePerFace;
                int triInFace = tri % trianglePerFace;
                int quad = triInFace / 2;
                int lin = quad / (res - 1);
                int col = quad % (res - 1);
                int triCorner = triInFace % 2;

                int2 cornerIndex0 = int2(col, lin);
                int2 cornerIndex1 = int2(col + 1, lin);
                int2 cornerIndex2 = int2(col, lin + 1);
                int2 cornerIndex3 = int2(col + 1, lin + 1);

                int2 chosen;
                if (triCorner == 0)
                {
                    if (id % 3 == 0) chosen = cornerIndex0;
                    else if (id % 3 == 1) chosen = cornerIndex2;
                    else chosen = cornerIndex1;
                }
                else
                {
                    if (id % 3 == 0) chosen = cornerIndex1;
                    else if (id % 3 == 1) chosen = cornerIndex2;
                    else chosen = cornerIndex3;
                }

                float fu = (float)chosen.x / (res - 1) * 2 - 1;
                float fv = (float)chosen.y / (res - 1) * 2 - 1;

                float3 cubePos = FaceCubePos(face, fu, fv);
                float3 spherePos = normalize(cubePos) * _Radius;
                float3 normalOS = normalize(cubePos);

                o.pos = UnityObjectToClipPos(float4(spherePos, 1));
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, normalOS));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = saturate(dot(normalize(i.normal), lightDir));
                return fixed4(_Color.rgb * (0.15 + ndotl * 0.85), _Color.a);
            }

            ENDCG
        }
    }

    FallBack "Diffuse"
}
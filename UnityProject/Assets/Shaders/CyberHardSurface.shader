Shader "Custom/CyberHardSurface"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.12, 0.14, 0.2, 1)
        _ShadowColor ("Shadow Tint", Color) = (0.05, 0.06, 0.12, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _RimColor ("Rim Color", Color) = (0.2, 0.9, 1.0, 1)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 2.8
        _RimStrength ("Rim Strength", Range(0, 2.5)) = 0.65
        _OutlineWidth ("Outline Width", Range(0, 0.02)) = 0.0025
        _OutlineColor ("Outline Color", Color) = (0.02, 0.03, 0.06, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _ShadowColor;
            fixed4 _EmissionColor;
            fixed4 _RimColor;
            float _RimPower;
            float _RimStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 H = normalize(L + V);

                float ndotl = dot(N, L) * 0.5 + 0.5;
                float toon = smoothstep(0.42, 0.52, ndotl);
                float spec = smoothstep(0.88, 0.94, dot(N, H)) * 0.45;
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;

                fixed3 baseCol = lerp(_Color.rgb * _ShadowColor.rgb, _Color.rgb * _LightColor0.rgb, toon);
                fixed3 finalCol = baseCol + spec * _RimColor.rgb + rim * _RimColor.rgb + _EmissionColor.rgb;
                return fixed4(finalCol, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            fixed4 _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    Fallback "Standard"
}

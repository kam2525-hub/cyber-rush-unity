Shader "Custom/CyberNeonGrid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.04, 0.05, 0.11, 1)
        [HDR] _GridColor ("Grid Neon Color", Color) = (0.12, 0.92, 1.0, 1)
        _GridScale ("Minor Grid Scale", Float) = 0.5
        _MajorDiv ("Major Grid Interval", Float) = 4.0
        _LineWidth ("Line Thickness", Range(0.01, 0.2)) = 0.045
        _FresnelPower ("Edge Glow Power", Range(1.0, 8.0)) = 3.0
        _FresnelStrength ("Edge Glow Strength", Range(0.0, 3.0)) = 0.85
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
            #pragma multi_compile_fwdbase
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
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            fixed4 _BaseColor;
            fixed4 _GridColor;
            float _GridScale;
            float _MajorDiv;
            float _LineWidth;
            float _FresnelPower;
            float _FresnelStrength;

            float GridMask(float2 uv, float width)
            {
                float2 cell = abs(frac(uv - 0.5) - 0.5);
                float2 deriv = max(fwidth(uv), float2(0.0005, 0.0005));
                float2 lines = smoothstep(deriv * 1.4, float2(0.0, 0.0), cell - width);
                return max(lines.x, lines.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                // Select dominant projection plane cleanly (platforms XZ vs walls XY/ZY)
                float3 an = abs(N);
                float2 p = an.y > 0.6 ? i.worldPos.xz : (an.x > 0.6 ? i.worldPos.zy : i.worldPos.xy);

                float minorG = GridMask(p * _GridScale, _LineWidth * 0.55) * 0.35;
                float majorG = GridMask((p * _GridScale) / max(_MajorDiv, 1.0), _LineWidth * 0.85);
                float grid = saturate(minorG + majorG);

                float ndotl = saturate(dot(N, L)) * 0.5 + 0.5;
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), 24.0) * 0.25;

                float distFade = saturate(1.0 - length(_WorldSpaceCameraPos - i.worldPos) / 140.0);
                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower) * _FresnelStrength;

                fixed3 surf = _BaseColor.rgb * (0.45 + 0.55 * ndotl) + spec * _GridColor.rgb;
                fixed3 finalCol = surf + _GridColor.rgb * (grid * (0.35 + 1.15 * distFade) + fresnel);
                return fixed4(finalCol, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Standard"
}

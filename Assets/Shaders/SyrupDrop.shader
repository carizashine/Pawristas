// Procedural shader for individual syrup drops.
// Self-shaded — does not depend on real scene lights — so drops look consistent
// regardless of whether the syrup minigame scene has lighting set up properly.
//
// Looks shiny via:
//   - Stylized fixed-direction key light + ambient floor
//   - Blinn-Phong specular highlight (Glossiness controls tightness)
//   - Fresnel rim (brighter edge when viewed glancingly)
//   - World-position-driven shimmer so each drop pulses independently

Shader "Custom/SyrupDrop"
{
    Properties
    {
        _Color           ("Drop Color",        Color)         = (0.82, 0.52, 0.10, 1)
        _Highlight       ("Highlight Color",   Color)         = (1.00, 1.00, 0.95, 1)
        _Glossiness      ("Glossiness",        Range(0, 1))   = 0.85
        _FresnelPower    ("Fresnel Power",     Range(0.5, 8)) = 3.0
        _FresnelStrength ("Fresnel Strength",  Range(0, 2))   = 1.2
        _ShimmerSpeed    ("Shimmer Speed",     Float)         = 2.0
        _ShimmerStrength ("Shimmer Strength",  Range(0, 1))   = 0.35
    }

    // ── URP SubShader ───────────────────────────────────────────────────
    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "SyrupDropURP"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Highlight;
                float  _Glossiness;
                float  _FresnelPower;
                float  _FresnelStrength;
                float  _ShimmerSpeed;
                float  _ShimmerStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetCameraPositionWS() - IN.positionWS);

                // Stylized fixed light direction — looks like overhead key + slight side fill.
                float3 L     = normalize(float3(0.30, 0.85, -0.30));
                float  NdotL = saturate(dot(N, L));

                // Half-lambert-ish so the unlit side is still readable.
                float3 baseCol = _Color.rgb * (0.55 + NdotL * 0.55);

                // Blinn-Phong specular.
                float3 H         = normalize(L + V);
                float  NdotH     = saturate(dot(N, H));
                float  specPower = lerp(16.0, 512.0, _Glossiness);
                float  spec      = pow(NdotH, specPower) * _Glossiness;

                // Fresnel rim — lights up the silhouette so drops feel glassy.
                float fres = 1.0 - saturate(dot(N, V));
                fres       = pow(fres, _FresnelPower) * _FresnelStrength;

                // Per-drop shimmer — phase varies by world position so drops don't sync.
                float t       = _Time.y;
                float shimmer = sin(t * _ShimmerSpeed + IN.positionWS.x * 4.0 + IN.positionWS.z * 3.7) * 0.5 + 0.5;
                shimmer       = lerp(1.0 - _ShimmerStrength, 1.0, shimmer);

                float3 final = baseCol * shimmer;
                final += spec * _Highlight.rgb;
                final += fres * _Highlight.rgb * 0.7;

                return half4(saturate(final), 1.0);
            }
            ENDHLSL
        }
    }

    // ── Built-in fallback (CG) ──────────────────────────────────────────
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 vertex  : SV_POSITION;
                float3 normalW : TEXCOORD0;
                float3 posW    : TEXCOORD1;
            };

            float4 _Color;
            float4 _Highlight;
            float  _Glossiness;
            float  _FresnelPower;
            float  _FresnelStrength;
            float  _ShimmerSpeed;
            float  _ShimmerStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.posW    = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vertex  = UnityObjectToClipPos(v.vertex);
                o.normalW = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N     = normalize(i.normalW);
                float3 V     = normalize(_WorldSpaceCameraPos - i.posW);
                float3 L     = normalize(float3(0.30, 0.85, -0.30));
                float  NdotL = saturate(dot(N, L));

                float3 baseCol = _Color.rgb * (0.55 + NdotL * 0.55);

                float3 H         = normalize(L + V);
                float  NdotH     = saturate(dot(N, H));
                float  specPower = lerp(16.0, 512.0, _Glossiness);
                float  spec      = pow(NdotH, specPower) * _Glossiness;

                float fres = 1.0 - saturate(dot(N, V));
                fres       = pow(fres, _FresnelPower) * _FresnelStrength;

                float t       = _Time.y;
                float shimmer = sin(t * _ShimmerSpeed + i.posW.x * 4.0 + i.posW.z * 3.7) * 0.5 + 0.5;
                shimmer       = lerp(1.0 - _ShimmerStrength, 1.0, shimmer);

                float3 final = baseCol * shimmer;
                final += spec * _Highlight.rgb;
                final += fres * _Highlight.rgb * 0.7;

                return fixed4(saturate(final), 1.0);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}

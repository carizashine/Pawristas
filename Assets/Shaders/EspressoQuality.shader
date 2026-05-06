// Custom procedural shader for the espresso quality reveal disc.
// _Quality:  0.0 = bad pour, 0.5 = alright, 1.0 = great
// Renders a circular crema-coffee surface with tier-based color, noise,
// crema ring, optional tiger stripes, and a warm shimmer at high quality.

Shader "Custom/EspressoQuality"
{
    Properties
    {
        _Quality       ("Quality (0=bad, 1=great)", Range(0, 1)) = 0.5
        _NoiseScale    ("Surface Noise Scale",      Float)        = 6.0
        _CremaWidth    ("Crema Ring Width",         Range(0, 0.4))= 0.18
        _ShimmerSpeed  ("Shimmer Speed",            Float)        = 1.0
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
            Name "EspressoQualityURP"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Quality;
                float _NoiseScale;
                float _CremaWidth;
                float _ShimmerSpeed;
            CBUFFER_END

            // ── Noise helpers ────────────────────────────────────────
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int n = 0; n < 4; n++)
                {
                    v += a * vnoise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Center UVs around (0,0); dist = 0 at center, 1 at disc edge.
                float2 centered = IN.uv - 0.5;
                float  dist     = length(centered) * 2.0;

                // Hard-clip the disc shape (works on a square Quad too).
                if (dist > 1.0) discard;

                // ── Three-tier color palettes ───────────────────────
                float3 badColor    = float3(0.55, 0.42, 0.28); // Pale watery brown
                float3 okColor     = float3(0.32, 0.20, 0.10); // Medium espresso
                float3 greatColor  = float3(0.10, 0.05, 0.02); // Rich dark espresso

                float3 badCrema    = float3(0.50, 0.38, 0.24); // Faint dull crema
                float3 okCrema     = float3(0.65, 0.45, 0.25); // Warm light crema
                float3 greatCrema  = float3(0.92, 0.68, 0.30); // Golden tiger crema

                float3 baseColor;
                float3 cremaColor;

                if (_Quality < 0.5)
                {
                    float k = _Quality * 2.0;
                    baseColor  = lerp(badColor, okColor,  k);
                    cremaColor = lerp(badCrema, okCrema,  k);
                }
                else
                {
                    float k = (_Quality - 0.5) * 2.0;
                    baseColor  = lerp(okColor,  greatColor, k);
                    cremaColor = lerp(okCrema,  greatCrema, k);
                }

                // ── Surface noise ──────────────────────────────────
                // Bad pour: chunky, uneven streaks. Great pour: smooth velvety surface.
                float t  = _Time.y;
                float n  = fbm(centered * _NoiseScale + t * 0.06);
                float n2 = fbm(centered * _NoiseScale * 2.5 - t * 0.04);

                float roughness  = lerp(1.0, 0.2, _Quality);
                float surfaceMod = (n - 0.5) * 0.5 * roughness;
                float3 surface   = baseColor + surfaceMod * float3(0.22, 0.14, 0.08);

                // ── Crema ring ─────────────────────────────────────
                float cremaStart = 1.0 - _CremaWidth;
                float cremaMask  = smoothstep(cremaStart - 0.05, cremaStart + 0.02, dist);
                cremaMask       *= smoothstep(1.0, 0.95, dist);

                // Tiger stripes in the crema (more pronounced as quality climbs)
                float angle      = atan2(centered.y, centered.x);
                float stripes    = sin(angle * 7.0 + n2 * 6.0 + t * 0.2) * 0.5 + 0.5;
                float stripeMix  = lerp(0.6, 1.2, stripes);
                cremaColor      *= lerp(1.0, stripeMix, _Quality * 0.7);

                // ── Composite ──────────────────────────────────────
                float3 final = lerp(surface, cremaColor, cremaMask);

                // Subtle inner crema wash that only appears for good pours
                float innerCrema = smoothstep(0.5, 0.0, dist) *
                                   saturate((_Quality - 0.5) * 2.0) * 0.15;
                final += innerCrema * cremaColor;

                // Warm shimmer for great quality only
                float shimmerAmt = saturate((_Quality - 0.7) / 0.3);
                float shimmer    = sin(t * _ShimmerSpeed + n * 6.28 + dist * 8.0) * 0.5 + 0.5;
                final += shimmer * shimmerAmt * 0.07 * float3(1.0, 0.85, 0.45);

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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            float _Quality;
            float _NoiseScale;
            float _CremaWidth;
            float _ShimmerSpeed;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int n = 0; n < 4; n++)
                {
                    v += a * vnoise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float dist = length(centered) * 2.0;
                if (dist > 1.0) discard;

                float3 badColor   = float3(0.55, 0.42, 0.28);
                float3 okColor    = float3(0.32, 0.20, 0.10);
                float3 greatColor = float3(0.10, 0.05, 0.02);
                float3 badCrema   = float3(0.50, 0.38, 0.24);
                float3 okCrema    = float3(0.65, 0.45, 0.25);
                float3 greatCrema = float3(0.92, 0.68, 0.30);

                float3 baseColor, cremaColor;
                if (_Quality < 0.5)
                {
                    float k = _Quality * 2.0;
                    baseColor  = lerp(badColor, okColor, k);
                    cremaColor = lerp(badCrema, okCrema, k);
                }
                else
                {
                    float k = (_Quality - 0.5) * 2.0;
                    baseColor  = lerp(okColor, greatColor, k);
                    cremaColor = lerp(okCrema, greatCrema, k);
                }

                float t  = _Time.y;
                float n  = fbm(centered * _NoiseScale + t * 0.06);
                float n2 = fbm(centered * _NoiseScale * 2.5 - t * 0.04);

                float roughness  = lerp(1.0, 0.2, _Quality);
                float surfaceMod = (n - 0.5) * 0.5 * roughness;
                float3 surface   = baseColor + surfaceMod * float3(0.22, 0.14, 0.08);

                float cremaStart = 1.0 - _CremaWidth;
                float cremaMask  = smoothstep(cremaStart - 0.05, cremaStart + 0.02, dist);
                cremaMask       *= smoothstep(1.0, 0.95, dist);

                float angle     = atan2(centered.y, centered.x);
                float stripes   = sin(angle * 7.0 + n2 * 6.0 + t * 0.2) * 0.5 + 0.5;
                float stripeMix = lerp(0.6, 1.2, stripes);
                cremaColor     *= lerp(1.0, stripeMix, _Quality * 0.7);

                float3 final = lerp(surface, cremaColor, cremaMask);

                float innerCrema = smoothstep(0.5, 0.0, dist) *
                                   saturate((_Quality - 0.5) * 2.0) * 0.15;
                final += innerCrema * cremaColor;

                float shimmerAmt = saturate((_Quality - 0.7) / 0.3);
                float shimmer    = sin(t * _ShimmerSpeed + n * 6.28 + dist * 8.0) * 0.5 + 0.5;
                final += shimmer * shimmerAmt * 0.07 * float3(1.0, 0.85, 0.45);

                return fixed4(saturate(final), 1.0);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}

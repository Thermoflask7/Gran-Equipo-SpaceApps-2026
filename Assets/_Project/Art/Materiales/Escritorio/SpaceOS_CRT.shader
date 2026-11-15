Shader "SpaceApps/SpaceOS CRT"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _ScanlineIntensity("Scanline Intensity", Range(0, 1)) = 0.35
        _ScanlineCount("Scanline Count", Range(50, 800)) = 240
        _PhosphorIntensity("Phosphor Mask", Range(0, 1)) = 0.22
        _VignetteIntensity("Vignette", Range(0, 1)) = 0.45
        _CornerRadius("Corner Radius", Range(0, 0.5)) = 0.08
        _BloomIntensity("Bloom / Glow", Range(0, 2)) = 0.28
        _FlickerIntensity("Flicker", Range(0, 0.2)) = 0.015
        _FlickerSpeed("Flicker Speed", Range(0, 30)) = 8
        _BarrelDistortion("Barrel Distortion", Range(0, 0.3)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _ScanlineIntensity;
                half _ScanlineCount;
                half _PhosphorIntensity;
                half _VignetteIntensity;
                half _CornerRadius;
                half _BloomIntensity;
                half _FlickerIntensity;
                half _FlickerSpeed;
                half _BarrelDistortion;
            CBUFFER_END

            float4 _BaseMap_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float2 ApplyBarrel(float2 uv, float barrel)
            {
                float2 c = uv * 2.0 - 1.0;
                float r2 = dot(c, c);
                c *= 1.0 + barrel * r2;
                return c * 0.5 + 0.5;
            }

            // Caja redondeada. p en -1..1, r radio (en esas unidades).
            float RoundedBoxMask(float2 p, float r)
            {
                r = max(r, 1e-4);
                float2 q = abs(p) - (1.0 - r);
                float sd = min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
                return 1.0 - smoothstep(0.0, 0.03, sd);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = ApplyBarrel(input.uv, _BarrelDistortion);
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half3 color = baseCol.rgb;

                // Bloom barato: brillo propio + 4 muestras vecinas.
                float2 texel = _BaseMap_TexelSize.xy * 2.0;
                half3 blur = color;
                blur += SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + float2(texel.x, 0)).rgb;
                blur += SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + float2(-texel.x, 0)).rgb;
                blur += SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + float2(0, texel.y)).rgb;
                blur += SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + float2(0, -texel.y)).rgb;
                blur *= 0.2;
                color += blur * _BloomIntensity;
                color += color * color * (_BloomIntensity * 0.35);

                // Mascara RGB tipo fosforo / aperture grille (sin branches).
                float triad = frac(uv.x * 270.0);
                half3 mask = half3(1.0, 0.2, 0.2);
                mask = lerp(mask, half3(0.2, 1.0, 0.2), step(0.333, triad));
                mask = lerp(mask, half3(0.2, 0.2, 1.0), step(0.666, triad));
                color = lerp(color, color * mask * 1.45, _PhosphorIntensity);

                // Scanlines.
                float sl = 0.5 - 0.5 * sin(uv.y * _ScanlineCount * 3.14159265);
                color *= 1.0 - _ScanlineIntensity * sl;

                // Flicker suave. Sobre negro (pantalla apagada) permanece negro.
                float t = _Time.y * _FlickerSpeed;
                float flick = 1.0 + _FlickerIntensity * (sin(t * 6.2831853) + 0.4 * sin(t * 13.1 + 1.7));
                color *= flick;

                // Vineta.
                float2 vc = uv - 0.5;
                float vig = 1.0 - _VignetteIntensity * saturate(dot(vc, vc) * 3.2);
                color *= vig;

                // Esquinas redondeadas (fuera de la caja = negro, como el bisel).
                float corner = RoundedBoxMask(uv * 2.0 - 1.0, _CornerRadius * 2.0);
                color *= corner;

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex VertDepth
            #pragma fragment FragDepth
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings VertDepth(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 FragDepth(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
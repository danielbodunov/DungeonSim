Shader "DungeonSim/Dungeon Grid Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _OcclusionMap("Baked Ambient Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        _NormalLightingStrength("Normal Lighting Strength", Range(0, 1)) = 0.4
        _ShapeLightDirection("Shape Light Direction", Vector) = (-0.35, 0.65, 0.85, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "DungeonGridForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            Blend One Zero
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            // These are supplied globally by DungeonLightingManager.
            TEXTURE2D(_DungeonLightTexture);
            SAMPLER(sampler_DungeonLightTexture);
            float4 _DungeonGridCellZero;
            float4 _DungeonGridStep;
            float4 _DungeonGridSize;
            float4 _DungeonAmbientColor;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half _OcclusionStrength;
                half _NormalLightingStrength;
                float4 _ShapeLightDirection;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = albedoSample.rgb * _BaseColor.rgb;
                half sampledOcclusion = SAMPLE_TEXTURE2D(
                    _OcclusionMap, sampler_OcclusionMap, input.uv).g;
                half occlusion = lerp(1.0h, sampledOcclusion, _OcclusionStrength);

                float2 safeStep = float2(
                    abs(_DungeonGridStep.x) < 0.0001 ? 1.0 : _DungeonGridStep.x,
                    abs(_DungeonGridStep.y) < 0.0001 ? 1.0 : _DungeonGridStep.y);
                float2 gridCoordinate =
                    (input.positionWS.xy - _DungeonGridCellZero.xy) / safeStep;
                float2 lightUv = (gridCoordinate + 0.5) / max(_DungeonGridSize.xy, 1.0);
                bool insideGrid = all(lightUv >= 0.0) && all(lightUv <= 1.0);
                half3 dungeonLight = insideGrid
                    ? SAMPLE_TEXTURE2D(
                        _DungeonLightTexture,
                        sampler_DungeonLightTexture,
                        saturate(lightUv)).rgb
                    : 0.0h;

                half3 emission = SAMPLE_TEXTURE2D(
                    _EmissionMap, sampler_EmissionMap, input.uv).rgb *
                    _EmissionColor.rgb;
                half3 lighting = _DungeonAmbientColor.rgb * occlusion + dungeonLight;
                half3 normalWS = normalize(input.normalWS);
                half3 shapeDirection = normalize((half3)_ShapeLightDirection.xyz);
                // Absolute N dot L keeps thin, double-sided props shaped from
                // either viewing side without treating them as translucent.
                half normalTerm = saturate(abs(dot(normalWS, shapeDirection)));
                half shapeLighting = lerp(0.65h, 1.15h, normalTerm);
                shapeLighting = lerp(1.0h, shapeLighting, _NormalLightingStrength);
                return half4(albedo * lighting * shapeLighting + emission, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}

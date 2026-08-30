Shader "DungeonSim/Rotation Safe Tile Atlas"
{
    Properties
    {
        [MainTexture] _BaseMap("Surface Atlas", 2D) = "white" {}
        [NoScaleOffset] _MaterialMaskAtlas("Material Mask Atlas", 2D) = "black" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _SurfaceLookup("Surface Lookup", 2D) = "black" {}
        [NoScaleOffset] _GroundSurfaceLookup("Ground Surface Lookup", 2D) = "black" {}
        [HideInInspector] _SurfaceLookupSize("Surface Lookup Size", Vector) = (257, 1, 0.00389105, 1)

        _EnableWorldSpace("Enable World Space", Float) = 1
        _WorldTiling("World Tiling", Float) = 3
        _PrimaryFamily("Primary Family", Float) = 0
        _SecondaryFamily("Secondary Family", Float) = 0
        _AccentFamily("Accent Family", Float) = 0
        _SpecialFamily("Special Family", Float) = 0
        _VisualSeed("Visual Seed", Float) = 0

        _UseGroundLayers("Use Ground Layers", Float) = 0
        _GroundLookupStartRow("Ground Lookup Start Row", Float) = 0
        _GroundTopY("Ground Top Y", Float) = 0
        _GroundCellWorldSize("Ground Cell World Size", Float) = 0.33333334
        _GroundCellScale("Ground Cell Scale", Float) = 3

        _LightSteps("Light Steps", Range(2, 16)) = 4
        _MinLight("Minimum Light", Range(0, 1)) = 0.25
        _LightExposure("Light Exposure", Range(0.01, 4)) = 1
        _OverbrightThreshold("Overbright Threshold", Range(0, 4)) = 0.9
        _OverbrightResponse("Overbright Response", Range(0.1, 4)) = 1.25
        _MaxOverbright("Maximum Overbright", Range(1, 4)) = 1.75
        _LightColorInfluence("Light Color Influence", Range(0, 1)) = 0.35
        _OverbrightColorInfluence("Overbright Color Influence", Range(0, 1)) = 0.8
        _HotWashStrength("Hot Wash Strength", Range(0, 3)) = 0.75
        _HotWashBlackPoint("Hot Wash Black Point", Range(0, 1)) = 0.05
        _HotWashFullPoint("Hot Wash Full Point", Range(0, 1)) = 0.3
        _HotWashColorInfluence("Hot Wash Color Influence", Range(0, 1)) = 0.9
        _AOIntensity("Vertex AO Intensity", Range(0, 1)) = 1
        [Enum(Off,0,On,1)] _EnableMaterialMask("Enable Material Mask", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity("Emission Intensity", Range(0, 16)) = 0
        _SpecularStrength("Specular Strength", Range(0, 4)) = 0
        _SpecularStylization("Specular Stylization", Range(0, 1)) = 1
        _SpecularSteps("Specular Steps", Range(2, 16)) = 4
        _SpecularLightDirection("Specular Light Direction", Vector) = (0.35, 0.65, -0.85, 0)
        _GlobalLightTint("Global Light Tint", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
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
            Name "RotationSafeForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull [_Cull]
            Blend One Zero
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Includes/DungeonPixelLitCore.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 surfaceSlot : TEXCOORD2;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 surfaceSlot : TEXCOORD3;
                half ao : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaterialMaskAtlas);
            SAMPLER(sampler_MaterialMaskAtlas);
            TEXTURE2D(_SurfaceLookup);
            SAMPLER(sampler_SurfaceLookup);
            TEXTURE2D(_GroundSurfaceLookup);
            SAMPLER(sampler_GroundSurfaceLookup);

            float4 _BaseMap_TexelSize;
            float4 _SurfaceLookup_TexelSize;
            float4 _GroundSurfaceLookup_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float4 _GlobalLightTint;
                float4 _SpecularLightDirection;
                float4 _SurfaceLookupSize;
                float _EnableWorldSpace;
                float _WorldTiling;
                float _PrimaryFamily;
                float _SecondaryFamily;
                float _AccentFamily;
                float _SpecialFamily;
                float _VisualSeed;
                float _UseGroundLayers;
                float _GroundLookupStartRow;
                float _GroundTopY;
                float _GroundCellWorldSize;
                float _GroundCellScale;
                float _LightSteps;
                float _MinLight;
                float _LightExposure;
                float _OverbrightThreshold;
                float _OverbrightResponse;
                float _MaxOverbright;
                float _LightColorInfluence;
                float _OverbrightColorInfluence;
                float _HotWashStrength;
                float _HotWashBlackPoint;
                float _HotWashFullPoint;
                float _HotWashColorInfluence;
                float _AOIntensity;
                float _EnableMaterialMask;
                float _EmissionIntensity;
                float _SpecularStrength;
                float _SpecularStylization;
                float _SpecularSteps;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.surfaceSlot = input.surfaceSlot;
                output.ao = input.color.r;
                return output;
            }

            float Hash01(float2 logicalCell, float layerKey)
            {
                return frac(sin(dot(
                    float4(logicalCell, layerKey, _VisualSeed),
                    float4(12.9898, 78.233, 37.719, 11.131))) * 43758.5453);
            }

            float4 ResolveAtlasRect(float3 positionWS, half3 normalWS, float2 surfaceSlot,
                out float2 localUV)
            {
                float3 normal = normalize(normalWS);
                float3 absoluteNormal = abs(normal);
                float2 projected;
                float role;

                if (absoluteNormal.z >= max(absoluteNormal.x, absoluteNormal.y))
                {
                    projected = positionWS.xy;
                    role = 0;
                }
                else if (absoluteNormal.y >= absoluteNormal.x)
                {
                    projected = float2(positionWS.x, normal.y >= 0 ? positionWS.z : -positionWS.z);
                    role = normal.y >= 0 ? 1 : 2;
                }
                else
                {
                    projected = float2(normal.x >= 0 ? positionWS.z : -positionWS.z, positionWS.y);
                    role = 3;
                }

                float2 scaledCoordinate = projected * max(_WorldTiling, 0.0001);
                float2 logicalCell = floor(scaledCoordinate);
                localUV = frac(scaledCoordinate);

                if (_UseGroundLayers > 0.5)
                {
                    scaledCoordinate = projected * max(_GroundCellScale, 0.0001);
                    logicalCell = floor(scaledCoordinate);
                    localUV = frac(scaledCoordinate);

                    float depth = max(0, floor(
                        ((_GroundTopY - positionWS.y) / max(_GroundCellWorldSize, 0.0001)) + 0.0001));
                    float depthColumn = depth > 255 ? 256 : depth;
                    float2 texel = _GroundSurfaceLookup_TexelSize.xy;
                    float2 bandUV = float2(
                        (depthColumn + 0.5) * texel.x,
                        (_GroundLookupStartRow + 0.5) * texel.y);
                    float band = floor(SAMPLE_TEXTURE2D_LOD(
                        _GroundSurfaceLookup, sampler_GroundSurfaceLookup, bandUV, 0).r + 0.5);
                    float rectRow = _GroundLookupStartRow + 1 + band * 2;
                    float choiceColumn = min(255, floor(
                        Hash01(logicalCell, depth + _GroundLookupStartRow * 17) * 256));
                    float2 choiceUV = float2(
                        (choiceColumn + 0.5) * texel.x,
                        (rectRow + 1.5) * texel.y);
                    float variant = floor(SAMPLE_TEXTURE2D_LOD(
                        _GroundSurfaceLookup, sampler_GroundSurfaceLookup, choiceUV, 0).r + 0.5);
                    float2 rectUV = float2(
                        (variant + 1.5) * texel.x,
                        (rectRow + 0.5) * texel.y);
                    return SAMPLE_TEXTURE2D_LOD(
                        _GroundSurfaceLookup, sampler_GroundSurfaceLookup, rectUV, 0);
                }

                float slot = clamp(floor(surfaceSlot.x + 0.5), 0, 3);
                float family = slot < 0.5
                    ? _PrimaryFamily
                    : (slot < 1.5 ? _SecondaryFamily : (slot < 2.5 ? _AccentFamily : _SpecialFamily));
                float rectRow = family * 8 + role * 2;
                float2 texel = _SurfaceLookup_TexelSize.xy;
                float choiceColumn = min(255, floor(Hash01(logicalCell, family + role * 17) * 256));
                float2 choiceUV = float2(
                    (choiceColumn + 0.5) * texel.x,
                    (rectRow + 1.5) * texel.y);
                float variant = floor(SAMPLE_TEXTURE2D_LOD(
                    _SurfaceLookup, sampler_SurfaceLookup, choiceUV, 0).r + 0.5);
                float2 rectUV = float2(
                    (variant + 1.5) * texel.x,
                    (rectRow + 0.5) * texel.y);
                return SAMPLE_TEXTURE2D_LOD(
                    _SurfaceLookup, sampler_SurfaceLookup, rectUV, 0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 localUV;
                float4 rect = ResolveAtlasRect(
                    input.positionWS, normalize(input.normalWS), input.surfaceSlot, localUV);
                float2 atlasUV = rect.xy + _BaseMap_TexelSize.xy * 0.5 +
                    localUV * max(rect.zw - _BaseMap_TexelSize.xy, 0);
                float2 selectedUV = _EnableWorldSpace > 0.5
                    ? atlasUV
                    : input.uv * max(_WorldTiling, 0.0001);
                half4 atlas = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, selectedUV, 0);
                half4 materialMask = half4(0, 1, 0, 0);
                if (_EnableMaterialMask > 0.5)
                {
                    materialMask = SAMPLE_TEXTURE2D_LOD(
                        _MaterialMaskAtlas,
                        sampler_MaterialMaskAtlas,
                        selectedUV,
                        0);
                }

                DungeonPixelLitSettings lighting;
                lighting.baseTint = _BaseColor;
                lighting.globalLightTint = _GlobalLightTint.rgb;
                lighting.emissionColor = _EmissionColor.rgb;
                lighting.specularLightDirection =
                    _SpecularLightDirection.xyz;
                lighting.lightSteps = _LightSteps;
                lighting.minimumLight = _MinLight;
                lighting.lightExposure = _LightExposure;
                lighting.overbrightThreshold = _OverbrightThreshold;
                lighting.overbrightResponse = _OverbrightResponse;
                lighting.maximumOverbright = _MaxOverbright;
                lighting.lightColorInfluence = _LightColorInfluence;
                lighting.overbrightColorInfluence =
                    _OverbrightColorInfluence;
                lighting.hotWashStrength = _HotWashStrength;
                lighting.hotWashBlackPoint = _HotWashBlackPoint;
                lighting.hotWashFullPoint = _HotWashFullPoint;
                lighting.hotWashColorInfluence = _HotWashColorInfluence;
                lighting.aoIntensity = _AOIntensity;
                lighting.emissionIntensity = _EmissionIntensity;
                lighting.specularStrength = _SpecularStrength;
                lighting.specularStylization = _SpecularStylization;
                lighting.specularSteps = _SpecularSteps;

                return DungeonPixelLitEvaluate(
                    atlas,
                    materialMask,
                    input.positionWS,
                    input.normalWS,
                    input.ao,
                    lighting);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}

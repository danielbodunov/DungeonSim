Shader "DungeonSim/Rotation Safe Tile Atlas"
{
    Properties
    {
        [MainTexture] _BaseMap("Surface Atlas", 2D) = "white" {}
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
        _AOIntensity("Vertex AO Intensity", Range(0, 1)) = 1
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
            TEXTURE2D(_SurfaceLookup);
            SAMPLER(sampler_SurfaceLookup);
            TEXTURE2D(_GroundSurfaceLookup);
            SAMPLER(sampler_GroundSurfaceLookup);
            TEXTURE2D(_DungeonLightTexture);
            SAMPLER(sampler_DungeonLightTexture);

            float4 _BaseMap_TexelSize;
            float4 _SurfaceLookup_TexelSize;
            float4 _GroundSurfaceLookup_TexelSize;
            float _GlobalLightIntensity;
            float _DungeonGlobalLightInitialized;
            float _DungeonLightingInitialized;
            float _DungeonLightingModeBlend;
            float4 _DungeonGridCellZero;
            float4 _DungeonGridStep;
            float4 _DungeonGridSize;
            float4 _DungeonAmbientColor;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlobalLightTint;
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
                float _AOIntensity;
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

            half3 SampleDungeonLighting(float3 positionWS)
            {
                if (_DungeonLightingInitialized < 0.5)
                    return 0;

                float2 safeStep = float2(
                    abs(_DungeonGridStep.x) < 0.0001 ? 1 : _DungeonGridStep.x,
                    abs(_DungeonGridStep.y) < 0.0001 ? 1 : _DungeonGridStep.y);
                float2 gridCoordinate =
                    (positionWS.xy - _DungeonGridCellZero.xy) / safeStep;
                float2 lightUV =
                    (gridCoordinate + 0.5) / max(_DungeonGridSize.xy, 1);
                bool insideGrid = all(lightUV >= 0) && all(lightUV <= 1);
                half3 localLight = insideGrid
                    ? SAMPLE_TEXTURE2D_LOD(
                        _DungeonLightTexture,
                        sampler_DungeonLightTexture,
                        saturate(lightUV),
                        0).rgb
                    : 0;
                return max(0, _DungeonAmbientColor.rgb) + localLight;
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

                half3 dungeonLighting = SampleDungeonLighting(input.positionWS);
                float lightAmount = saturate(dot(
                    dungeonLighting,
                    half3(0.2126, 0.7152, 0.0722)));
                float steps = max(2, round(_LightSteps));
                float quantized = round(lightAmount * (steps - 1)) / (steps - 1);
                float localLightMultiplier = lerp(
                    saturate(_MinLight), 1, quantized);
                float vertexAO = lerp(1, saturate(input.ao), saturate(_AOIntensity));
                float presentationBrightness = _DungeonGlobalLightInitialized > 0.5
                    ? max(0, _GlobalLightIntensity)
                    : 1;
                float presentationLighting = lerp(
                    1,
                    localLightMultiplier,
                    saturate(_DungeonLightingModeBlend));

                half3 color = atlas.rgb * _BaseColor.rgb * _GlobalLightTint.rgb *
                    presentationLighting * presentationBrightness * vertexAO;
                return half4(color, atlas.a * _BaseColor.a);
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

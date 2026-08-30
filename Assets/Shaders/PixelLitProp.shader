Shader "DungeonSim/Pixel Lit Prop"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Tint", Color) = (1,1,1,1)
        [Toggle] _EnableMaterialMask("Enable Material Mask", Float) = 0
        [NoScaleOffset] _MaterialMask("Material Mask (R Emission, G Roughness, B Metallic)", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionIntensity("Emission Intensity", Range(0, 8)) = 0
        _SpecularStrength("Specular Strength", Range(0, 4)) = 0
        _SpecularStylization("Specular Stylization", Range(0, 1)) = 1
        _SpecularSteps("Specular Steps", Range(2, 16)) = 4
        _SpecularLightDirection("Specular Light Direction", Vector) = (0.35,0.65,-0.85,0)
        _LightSteps("Light Steps", Range(2, 16)) = 4
        _MinLight("Minimum Light", Range(0, 1)) = 0.25
        _LightExposure("Light Exposure", Range(0.01, 8)) = 1
        _OverbrightThreshold("Overbright Threshold", Range(0, 8)) = 0.9
        _OverbrightResponse("Overbright Response", Range(0.1, 8)) = 1.25
        _MaxOverbright("Maximum Overbright", Range(1, 8)) = 1.75
        _LightColorInfluence("Light Color Influence", Range(0, 1)) = 0.35
        _OverbrightColorInfluence("Overbright Color Influence", Range(0, 1)) = 0.8
        _HotWashStrength("Hot Wash Strength", Range(0, 4)) = 0.75
        _HotWashBlackPoint("Hot Wash Black Point", Range(0, 1)) = 0.05
        _HotWashFullPoint("Hot Wash Full Point", Range(0, 1)) = 0.3
        _HotWashColorInfluence("Hot Wash Color Influence", Range(0, 1)) = 0.9
        _AOIntensity("Vertex AO Intensity", Range(0, 1)) = 1
        [HDR] _GlobalLightTint("Global Light Tint", Color) = (1,1,1,1)
        [Toggle] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull [_Cull]
            ZWrite On

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
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half ao : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaterialMask);
            SAMPLER(sampler_MaterialMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _GlobalLightTint;
                float4 _SpecularLightDirection;
                float _EnableMaterialMask;
                float _EmissionIntensity;
                float _SpecularStrength;
                float _SpecularStylization;
                float _SpecularSteps;
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
                float _AlphaClip;
                float _Cutoff;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.ao = input.color.r;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 surfaceSample = SAMPLE_TEXTURE2D_LOD(
                    _BaseMap, sampler_BaseMap, input.uv, 0);
                if (_AlphaClip > 0.5)
                    clip(surfaceSample.a * _BaseColor.a - _Cutoff);

                half4 materialMask = half4(0, 1, 0, 0);
                if (_EnableMaterialMask > 0.5)
                {
                    materialMask = SAMPLE_TEXTURE2D_LOD(
                        _MaterialMask, sampler_MaterialMask, input.uv, 0);
                }

                DungeonPixelLitSettings lighting;
                lighting.baseTint = _BaseColor;
                lighting.globalLightTint = _GlobalLightTint.rgb;
                lighting.emissionColor = _EmissionColor.rgb;
                lighting.specularLightDirection = _SpecularLightDirection.xyz;
                lighting.lightSteps = _LightSteps;
                lighting.minimumLight = _MinLight;
                lighting.lightExposure = _LightExposure;
                lighting.overbrightThreshold = _OverbrightThreshold;
                lighting.overbrightResponse = _OverbrightResponse;
                lighting.maximumOverbright = _MaxOverbright;
                lighting.lightColorInfluence = _LightColorInfluence;
                lighting.overbrightColorInfluence = _OverbrightColorInfluence;
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
                    surfaceSample, materialMask, input.positionWS,
                    normalize(input.normalWS), input.ao, lighting);
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
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaterialMask); SAMPLER(sampler_MaterialMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _GlobalLightTint;
                float4 _SpecularLightDirection;
                float _EnableMaterialMask;
                float _EmissionIntensity;
                float _SpecularStrength;
                float _SpecularStylization;
                float _SpecularSteps;
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
                float _AlphaClip;
                float _Cutoff;
            CBUFFER_END
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                if (_AlphaClip > 0.5)
                    clip(SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, input.uv, 0).a * _BaseColor.a - _Cutoff);
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaterialMask); SAMPLER(sampler_MaterialMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half4 _GlobalLightTint;
                float4 _SpecularLightDirection;
                float _EnableMaterialMask;
                float _EmissionIntensity;
                float _SpecularStrength;
                float _SpecularStylization;
                float _SpecularSteps;
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
                float _AlphaClip;
                float _Cutoff;
            CBUFFER_END
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                if (_AlphaClip > 0.5)
                    clip(SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, input.uv, 0).a * _BaseColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}

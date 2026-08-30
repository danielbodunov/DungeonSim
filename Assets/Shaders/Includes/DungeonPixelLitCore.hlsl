#ifndef DUNGEON_PIXEL_LIT_CORE_INCLUDED
#define DUNGEON_PIXEL_LIT_CORE_INCLUDED

TEXTURE2D(_DungeonLightTexture);
TEXTURE2D(_DungeonPreviousLightTexture);

float _GlobalLightIntensity;
float _DungeonGlobalLightInitialized;
float _DungeonLightingInitialized;
float _DungeonLightingModeBlend;
float _DungeonLightTextureBlend;
float _DungeonLightingPixelsPerCell;
float _DungeonLightingPropagationSamplesPerCell;
float4 _DungeonGridCellZero;
float4 _DungeonGridStep;
float4 _DungeonGridSize;
float4 _DungeonAmbientColor;

struct DungeonPixelLitSettings
{
    half4 baseTint;
    half3 globalLightTint;
    half3 emissionColor;
    half3 specularLightDirection;
    float lightSteps;
    float minimumLight;
    float lightExposure;
    float overbrightThreshold;
    float overbrightResponse;
    float maximumOverbright;
    float lightColorInfluence;
    float overbrightColorInfluence;
    float hotWashStrength;
    float hotWashBlackPoint;
    float hotWashFullPoint;
    float hotWashColorInfluence;
    float aoIntensity;
    float emissionIntensity;
    float specularStrength;
    float specularStylization;
    float specularSteps;
};

half3 DungeonPixelLitSampleLocalLighting(float3 positionWS)
{
    if (_DungeonLightingInitialized < 0.5)
        return 0;

    float2 safeStep = float2(
        abs(_DungeonGridStep.x) < 0.0001 ? 1 : _DungeonGridStep.x,
        abs(_DungeonGridStep.y) < 0.0001 ? 1 : _DungeonGridStep.y);
    float2 gridCoordinate =
        (positionWS.xy - _DungeonGridCellZero.xy) / safeStep;
    float pixelsPerCell = max(1, round(_DungeonLightingPixelsPerCell));
    gridCoordinate =
        (floor((gridCoordinate + 0.5) * pixelsPerCell) + 0.5) /
        pixelsPerCell - 0.5;
    float2 lightUV =
        (gridCoordinate + 0.5) / max(_DungeonGridSize.xy, 1);
    bool insideGrid = all(lightUV >= 0) && all(lightUV <= 1);
    if (!insideGrid)
        return 0;

    float propagationSamplesPerCell = max(
        1, round(_DungeonLightingPropagationSamplesPerCell));
    float2 propagationSize = max(
        _DungeonGridSize.xy * propagationSamplesPerCell,
        1);
    float2 sampleCoordinate =
        (gridCoordinate + 0.5) * propagationSamplesPerCell - 0.5;
    float2 sampleBase = floor(sampleCoordinate);
    float2 sampleFraction = frac(sampleCoordinate);
    float2 sampleA = clamp(sampleBase, 0, propagationSize - 1);
    float2 sampleB = clamp(
        sampleBase + float2(1, 0), 0, propagationSize - 1);
    float2 sampleC = clamp(
        sampleBase + float2(0, 1), 0, propagationSize - 1);
    float2 sampleD = clamp(
        sampleBase + float2(1, 1), 0, propagationSize - 1);
    int2 texelA = int2(sampleA);
    int2 texelB = int2(sampleB);
    int2 texelC = int2(sampleC);
    int2 texelD = int2(sampleD);

    half3 previousA = LOAD_TEXTURE2D_LOD(
        _DungeonPreviousLightTexture, texelA, 0).rgb;
    half3 previousB = LOAD_TEXTURE2D_LOD(
        _DungeonPreviousLightTexture, texelB, 0).rgb;
    half3 previousC = LOAD_TEXTURE2D_LOD(
        _DungeonPreviousLightTexture, texelC, 0).rgb;
    half3 previousD = LOAD_TEXTURE2D_LOD(
        _DungeonPreviousLightTexture, texelD, 0).rgb;
    half3 previousLight = lerp(
        lerp(previousA, previousB, sampleFraction.x),
        lerp(previousC, previousD, sampleFraction.x),
        sampleFraction.y);

    half3 currentA = LOAD_TEXTURE2D_LOD(
        _DungeonLightTexture, texelA, 0).rgb;
    half3 currentB = LOAD_TEXTURE2D_LOD(
        _DungeonLightTexture, texelB, 0).rgb;
    half3 currentC = LOAD_TEXTURE2D_LOD(
        _DungeonLightTexture, texelC, 0).rgb;
    half3 currentD = LOAD_TEXTURE2D_LOD(
        _DungeonLightTexture, texelD, 0).rgb;
    half3 currentLight = lerp(
        lerp(currentA, currentB, sampleFraction.x),
        lerp(currentC, currentD, sampleFraction.x),
        sampleFraction.y);
    return lerp(
        previousLight,
        currentLight,
        saturate(_DungeonLightTextureBlend));
}

half4 DungeonPixelLitEvaluate(
    half4 surfaceSample,
    half4 materialMask,
    float3 positionWS,
    half3 normalWS,
    half vertexAoInput,
    DungeonPixelLitSettings settings)
{
    half3 localLighting = DungeonPixelLitSampleLocalLighting(positionWS);
    half3 totalLighting = max(0, _DungeonAmbientColor.rgb) + localLighting;
    float lightEnergy = max(0, dot(
        totalLighting,
        half3(0.2126, 0.7152, 0.0722)));
    float shapedLight = 1 - exp(
        -lightEnergy * max(0.01, settings.lightExposure));
    float steps = max(2, round(settings.lightSteps));
    float quantized = round(saturate(shapedLight) * (steps - 1)) /
        (steps - 1);
    float localLightMultiplier = lerp(
        saturate(settings.minimumLight), 1, quantized);
    float vertexAO = lerp(
        1,
        saturate(vertexAoInput),
        saturate(settings.aoIntensity));
    float presentationBrightness = _DungeonGlobalLightInitialized > 0.5
        ? max(0, _GlobalLightIntensity)
        : 1;
    float presentationLighting = lerp(
        1,
        localLightMultiplier,
        saturate(_DungeonLightingModeBlend));
    half maximumLocalChannel = max(
        localLighting.r,
        max(localLighting.g, localLighting.b));
    half3 localTint = maximumLocalChannel > 0.0001
        ? localLighting / maximumLocalChannel
        : half3(1, 1, 1);
    half3 stylizedTint = lerp(
        half3(1, 1, 1),
        localTint,
        saturate(settings.lightColorInfluence) *
            saturate(_DungeonLightingModeBlend));
    float localEnergy = max(0, dot(
        localLighting,
        half3(0.2126, 0.7152, 0.0722)));
    float excessEnergy = max(
        0, localEnergy - max(0, settings.overbrightThreshold));
    float overbrightT = 1 - exp(
        -excessEnergy * max(0.1, settings.overbrightResponse));
    float maximumOverbright = max(1, settings.maximumOverbright);
    float overbright = lerp(1, maximumOverbright, overbrightT);
    float hotAmount = saturate(
        (overbright - 1) / max(maximumOverbright - 1, 0.0001));
    half3 multiplicativeHotTint = lerp(
        half3(1, 1, 1),
        localTint,
        saturate(settings.overbrightColorInfluence) * hotAmount *
            saturate(_DungeonLightingModeBlend));
    float presentationOverbright = lerp(
        1, overbright, saturate(_DungeonLightingModeBlend));

    half3 baseLitColor =
        surfaceSample.rgb * settings.baseTint.rgb *
        settings.globalLightTint * presentationLighting * stylizedTint *
        multiplicativeHotTint * presentationOverbright *
        presentationBrightness * vertexAO;
    float surfaceLuminance = max(0, dot(
        surfaceSample.rgb,
        half3(0.2126, 0.7152, 0.0722)));
    float blackPoint = saturate(settings.hotWashBlackPoint);
    float fullPoint = max(
        blackPoint + 0.0001,
        saturate(settings.hotWashFullPoint));
    float washMask = smoothstep(
        blackPoint, fullPoint, surfaceLuminance);
    half3 hotColor = lerp(
        half3(1, 1, 1),
        localTint,
        saturate(settings.hotWashColorInfluence));
    half3 hotWash = hotColor * overbrightT * washMask *
        max(0, settings.hotWashStrength) *
        saturate(_DungeonLightingModeBlend) *
        presentationBrightness * vertexAO;

    float roughness = saturate(materialMask.g);
    float metallic = saturate(materialMask.b);
    half3 viewDirection = GetWorldSpaceNormalizeViewDir(positionWS);
    half3 specularLightDirection = normalize(
        settings.specularLightDirection);
    half3 halfDirection = normalize(
        viewDirection + specularLightDirection);
    float normalHalf = saturate(dot(normalize(normalWS), halfDirection));
    float smoothness = 1 - roughness;
    float specularPower = lerp(4, 128, smoothness * smoothness);
    float smoothSpecular = pow(normalHalf, specularPower) *
        lerp(0.35, 1, smoothness);
    float specularSteps = max(2, round(settings.specularSteps));
    float quantizedSpecular = round(
        saturate(smoothSpecular) * (specularSteps - 1)) /
        (specularSteps - 1);
    float styledSpecular = lerp(
        smoothSpecular,
        quantizedSpecular,
        saturate(settings.specularStylization));
    float specularLight = 1 - exp(
        -localEnergy * max(0.01, settings.lightExposure));
    half3 specularTint = lerp(
        half3(0.04, 0.04, 0.04),
        surfaceSample.rgb * settings.baseTint.rgb,
        metallic);
    half3 specularColor = specularTint * styledSpecular *
        specularLight * max(0, settings.specularStrength) *
        lerp(1, 2, metallic) *
        lerp(
            half3(1, 1, 1),
            localTint,
            saturate(settings.lightColorInfluence)) *
        saturate(_DungeonLightingModeBlend) *
        presentationBrightness * vertexAO;

    half3 emission = materialMask.r * settings.emissionColor *
        max(0, settings.emissionIntensity);

    return half4(
        baseLitColor + hotWash + specularColor + emission,
        surfaceSample.a * settings.baseTint.a);
}

#endif

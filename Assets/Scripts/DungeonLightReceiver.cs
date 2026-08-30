using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Adapts opaque renderer materials below this object to the dungeon light-grid
/// shader. The generated materials preserve common albedo, AO, and emission
/// properties and are shared between receivers that use the same source material.
/// </summary>
[DisallowMultipleComponent]
public class DungeonLightReceiver : MonoBehaviour
{
    const string DungeonShaderName = "DungeonSim/Dungeon Grid Lit";
    const string PixelLitPropShaderName = "DungeonSim/Pixel Lit Prop";

    static readonly Dictionary<Material, Material> generatedMaterials = new();
    static bool missingShaderWasLogged;

    [SerializeField, Tooltip("When assigned, this material replaces every opaque material below this receiver.")]
    Material materialOverride;
    [SerializeField] bool includeInactiveRenderers = true;
    [SerializeField, Tooltip("Transparent materials such as flames and particles retain their original shader.")]
    bool skipTransparentMaterials = true;
    [SerializeField, Tooltip("Restore original renderer materials when this component is disabled.")]
    bool restoreMaterialsOnDisable = true;

    readonly List<RendererState> rendererStates = new();

    sealed class RendererState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    public Material MaterialOverride => materialOverride;

    void OnEnable()
    {
        ApplyReceiverMaterials();
    }

    void Start()
    {
        // Also catches prototype renderers created by another component's Awake.
        RefreshRenderers();
    }

    void OnDisable()
    {
        if (restoreMaterialsOnDisable)
            RestoreOriginalMaterials();
        else
            rendererStates.Clear();
    }

    public void SetMaterialOverride(Material value)
    {
        if (materialOverride == value)
            return;

        materialOverride = value;
        if (isActiveAndEnabled)
            RefreshRenderers();
    }

    [ContextMenu("Refresh Dungeon Light Renderers")]
    public void RefreshRenderers()
    {
        RestoreOriginalMaterials();
        if (isActiveAndEnabled)
            ApplyReceiverMaterials();
    }

    void ApplyReceiverMaterials()
    {
        Shader dungeonShader = materialOverride == null
            ? Shader.Find(DungeonShaderName)
            : materialOverride.shader;
        if (dungeonShader == null)
        {
            if (!missingShaderWasLogged)
            {
                Debug.LogError(
                    $"DungeonLightReceiver could not find shader '{DungeonShaderName}'.",
                    this);
                missingShaderWasLogged = true;
            }
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(
            includeInactiveRenderers);
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null ||
                targetRenderer.GetComponentInParent<DungeonLightReceiver>(true) != this)
            {
                continue;
            }

            Material[] originals = targetRenderer.sharedMaterials;
            Material[] replacements = new Material[originals.Length];
            bool changed = false;
            for (int i = 0; i < originals.Length; i++)
            {
                Material original = originals[i];
                Material replacement = GetReplacementMaterial(original, dungeonShader);
                replacements[i] = replacement;
                changed |= replacement != original;
            }

            if (!changed)
                continue;

            rendererStates.Add(new RendererState
            {
                renderer = targetRenderer,
                originalMaterials = originals
            });
            targetRenderer.sharedMaterials = replacements;
        }
    }

    Material GetReplacementMaterial(Material original, Shader dungeonShader)
    {
        if (original == null)
            return null;
        if (skipTransparentMaterials && IsTransparent(original))
            return original;
        if (materialOverride != null)
            return materialOverride;
        if (original.shader != null &&
            original.shader.name == PixelLitPropShaderName)
        {
            return original;
        }
        if (original.shader == dungeonShader)
            return original;
        if (generatedMaterials.TryGetValue(original, out Material generated) &&
            generated != null)
        {
            return generated;
        }

        generated = new Material(dungeonShader)
        {
            name = $"{original.name} (Dungeon Grid)",
            hideFlags = HideFlags.DontSave,
            enableInstancing = original.enableInstancing,
            doubleSidedGI = original.doubleSidedGI,
            globalIlluminationFlags = original.globalIlluminationFlags,
            renderQueue = original.renderQueue
        };
        CopyCommonProperties(original, generated);
        // Generated ladders and platforms contain thin surfaces. Rendering both
        // sides avoids back-face disappearance that reads as transparency.
        if (generated.HasProperty("_Cull"))
            generated.SetFloat("_Cull", (float)CullMode.Off);
        generatedMaterials[original] = generated;
        return generated;
    }

    static bool IsTransparent(Material material)
    {
        if (material.renderQueue >= (int)RenderQueue.Transparent)
            return true;
        return material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f;
    }

    static void CopyCommonProperties(Material source, Material destination)
    {
        string baseTextureProperty = source.HasProperty("_BaseMap")
            ? "_BaseMap"
            : source.HasProperty("_MainTex") ? "_MainTex" : null;
        if (baseTextureProperty != null)
        {
            destination.SetTexture("_BaseMap", source.GetTexture(baseTextureProperty));
            destination.SetTextureScale(
                "_BaseMap", source.GetTextureScale(baseTextureProperty));
            destination.SetTextureOffset(
                "_BaseMap", source.GetTextureOffset(baseTextureProperty));
        }

        if (source.HasProperty("_BaseColor"))
            destination.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        else if (source.HasProperty("_Color"))
            destination.SetColor("_BaseColor", source.GetColor("_Color"));

        if (source.HasProperty("_OcclusionMap"))
            destination.SetTexture("_OcclusionMap", source.GetTexture("_OcclusionMap"));
        if (source.HasProperty("_OcclusionStrength"))
            destination.SetFloat(
                "_OcclusionStrength", source.GetFloat("_OcclusionStrength"));

        if (source.HasProperty("_EmissionMap"))
            destination.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
        if (source.HasProperty("_EmissionColor"))
            destination.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
    }

    void RestoreOriginalMaterials()
    {
        foreach (RendererState state in rendererStates)
            if (state.renderer != null)
                state.renderer.sharedMaterials = state.originalMaterials;
        rendererStates.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetMaterialCache()
    {
        generatedMaterials.Clear();
        missingShaderWasLogged = false;
    }
}

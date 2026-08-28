using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DungeonSurfaceLookupGenerator
{
    const string AtlasPath = "Assets/Materials/DungeonAtlas.png";
    const string LookupPath = "Assets/Resources/DungeonSurfaceLookup.asset";
    const string GroundLookupPath = "Assets/Resources/DungeonGroundSurfaceLookup.asset";
    const string MaterialPath = "Assets/Assets/DungeonTiles/RotationSafeTileAtlas.mat";
    const int SpriteSize = 32;
    const int MaxVariants = 16;
    const int MaxBakedGroundDepth = 255;
    const int WeightedChoiceSlots = 256;

    [InitializeOnLoadMethod]
    static void BuildInitialLookupWhenMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(LookupPath) == null ||
            AssetDatabase.LoadAssetAtPath<Texture2D>(GroundLookupPath) == null)
            EditorApplication.delayCall += Rebuild;
    }

    [MenuItem("Tools/Dungeon/Rebuild Surface Family Lookup")]
    public static void Rebuild()
    {
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlas == null)
            throw new InvalidOperationException($"Dungeon surface atlas is missing at {AtlasPath}.");

        DungeonSurfaceFamily[] families = AssetDatabase.FindAssets("t:DungeonSurfaceFamily")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<DungeonSurfaceFamily>(path))
            .OrderBy(family => family.StableId, StringComparer.Ordinal)
            .ToArray();
        if (families.Length == 0)
            throw new InvalidOperationException("Create at least one DungeonSurfaceFamily before rebuilding the lookup.");

        Texture2D lookup = new Texture2D(WeightedChoiceSlots + 1, families.Length * 8,
            TextureFormat.RGBAFloat, false, true)
        {
            name = "DungeonSurfaceLookup",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] clear = Enumerable.Repeat(Color.clear, lookup.width * lookup.height).ToArray();
        lookup.SetPixels(clear);

        var errors = new List<string>();
        for (int familyIndex = 0; familyIndex < families.Length; familyIndex++)
        {
            DungeonSurfaceFamily family = families[familyIndex];
            family.SetLookupIndex(familyIndex);
            EditorUtility.SetDirty(family);
            for (int roleIndex = 0; roleIndex < 4; roleIndex++)
            {
                IReadOnlyList<DungeonWeightedSpriteVariant> variants = family.GetVariants((DungeonSurfaceRole)roleIndex);
                int rectRow = familyIndex * 8 + roleIndex * 2;
                int choiceRow = rectRow + 1;
                if (variants.Count == 0)
                {
                    errors.Add($"{family.name}: {(DungeonSurfaceRole)roleIndex} has no variants.");
                    continue;
                }
                if (variants.Count > MaxVariants)
                {
                    errors.Add($"{family.name}: {(DungeonSurfaceRole)roleIndex} exceeds {MaxVariants} variants.");
                    continue;
                }

                float totalWeight = 0f;
                int firstValidVariant = -1;
                for (int variantIndex = 0; variantIndex < variants.Count; variantIndex++)
                {
                    DungeonWeightedSpriteVariant variant = variants[variantIndex];
                    Sprite sprite = variant.Sprite;
                    if (variant.Weight < 0f)
                        errors.Add($"{family.name}: {sprite?.name ?? $"variant {variantIndex}"} has a negative weight.");
                    if (!ValidateSprite(sprite, atlas, family, (DungeonSurfaceRole)roleIndex, errors))
                        continue;
                    if (firstValidVariant < 0)
                        firstValidVariant = variantIndex;
                    totalWeight += Mathf.Max(0f, variant.Weight);
                    Rect rect = sprite.rect;
                    lookup.SetPixel(variantIndex + 1, rectRow,
                        new Color(rect.x / atlas.width, rect.y / atlas.height,
                            rect.width / atlas.width, rect.height / atlas.height));
                }
                lookup.SetPixel(0, rectRow, new Color(variants.Count, totalWeight, 0, 0));
                if (totalWeight <= 0f && firstValidVariant >= 0)
                    Debug.LogWarning($"{family.name}/{(DungeonSurfaceRole)roleIndex}: all weights are zero; using the first valid Sprite as fallback.", family);
                for (int choice = 0; choice < WeightedChoiceSlots; choice++)
                {
                    int selected = totalWeight > 0f
                        ? SelectWeightedVariant(variants, ((choice + 0.5f) / WeightedChoiceSlots) * totalWeight)
                        : Mathf.Max(0, firstValidVariant);
                    lookup.SetPixel(choice, choiceRow, new Color(selected, 0, 0, 0));
                }
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("Surface lookup was not generated:\n- " + string.Join("\n- ", errors));

        foreach (DungeonSurfaceFamily family in families)
        {
            family.SetGeneratedLookupHash(ComputeSurfaceFamilyHash(family));
            EditorUtility.SetDirty(family);
        }

        lookup.Apply(false, false);
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(LookupPath);
        if (existing == null)
            AssetDatabase.CreateAsset(lookup, LookupPath);
        else
        {
            EditorUtility.CopySerialized(lookup, existing);
            UnityEngine.Object.DestroyImmediate(lookup);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Texture2D savedLookup = AssetDatabase.LoadAssetAtPath<Texture2D>(LookupPath);
        if (material != null)
        {
            material.SetTexture("_BaseMap", atlas);
            material.SetTexture("_SurfaceLookup", savedLookup);
            material.SetVector("_SurfaceLookupSize", new Vector4(savedLookup.width, savedLookup.height,
                1f / savedLookup.width, 1f / savedLookup.height));
            EditorUtility.SetDirty(material);
        }

        RebuildGroundLookup(atlas, material);

        AssetDatabase.SaveAssets();
        Debug.Log($"Generated {LookupPath} for {families.Length} surface families.");
    }

    static void RebuildGroundLookup(Texture2D atlas, Material material)
    {
        DungeonGroundSurfaceFamily[] families = AssetDatabase.FindAssets("t:DungeonGroundSurfaceFamily")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<DungeonGroundSurfaceFamily>(path))
            .OrderBy(family => family.StableId, StringComparer.Ordinal)
            .ToArray();
        if (families.Length == 0)
            throw new InvalidOperationException("Create at least one DungeonGroundSurfaceFamily before rebuilding the lookup.");

        int rowCount = families.Sum(family => 1 + family.Bands.Count * 2);
        Texture2D lookup = new Texture2D(MaxBakedGroundDepth + 2, rowCount,
            TextureFormat.RGBAFloat, false, true)
        {
            name = "DungeonGroundSurfaceLookup",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        lookup.SetPixels(Enumerable.Repeat(Color.clear, lookup.width * lookup.height).ToArray());

        var errors = new List<string>();
        int startRow = 0;
        foreach (DungeonGroundSurfaceFamily family in families)
        {
            family.SetLookupStartRow(startRow);
            EditorUtility.SetDirty(family);
            ValidateAndWriteGroundFamily(family, atlas, lookup, startRow, errors);
            startRow += 1 + family.Bands.Count * 2;
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("Ground surface lookup was not generated:\n- " + string.Join("\n- ", errors));

        foreach (DungeonGroundSurfaceFamily family in families)
        {
            family.SetGeneratedLookupHash(ComputeGroundFamilyHash(family));
            EditorUtility.SetDirty(family);
        }

        lookup.Apply(false, false);
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundLookupPath);
        if (existing == null)
            AssetDatabase.CreateAsset(lookup, GroundLookupPath);
        else
        {
            EditorUtility.CopySerialized(lookup, existing);
            UnityEngine.Object.DestroyImmediate(lookup);
        }

        Texture2D savedLookup = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundLookupPath);
        if (material != null)
        {
            material.SetTexture("_GroundSurfaceLookup", savedLookup);
            EditorUtility.SetDirty(material);
        }
        Debug.Log($"Generated {GroundLookupPath} for {families.Length} ground families.");
    }

    static void ValidateAndWriteGroundFamily(DungeonGroundSurfaceFamily family, Texture2D atlas,
        Texture2D lookup, int startRow, ICollection<string> errors)
    {
        if (family.Bands.Count == 0)
        {
            errors.Add($"{family.name}: has no ground layer bands.");
            return;
        }

        int fallbackIndex = -1;
        int expectedDepth = 0;
        for (int bandIndex = 0; bandIndex < family.Bands.Count; bandIndex++)
        {
            DungeonGroundLayerBand band = family.Bands[bandIndex];
            string label = string.IsNullOrWhiteSpace(band.DisplayName) ? $"Band {bandIndex}" : band.DisplayName;
            if (band.MinDepth < 0)
                errors.Add($"{family.name}/{label}: minimum depth cannot be negative.");
            if (!band.Unbounded && band.MaxDepth < band.MinDepth)
                errors.Add($"{family.name}/{label}: maximum depth is below minimum depth.");
            if (band.MinDepth != expectedDepth)
                errors.Add($"{family.name}/{label}: expected minimum depth {expectedDepth}; ranges must be contiguous and non-overlapping.");
            if (band.Unbounded)
            {
                if (fallbackIndex >= 0)
                    errors.Add($"{family.name}: contains more than one unbounded fallback band.");
                if (bandIndex != family.Bands.Count - 1)
                    errors.Add($"{family.name}/{label}: the unbounded fallback must be the final band.");
                fallbackIndex = bandIndex;
            }
            else
            {
                if (band.MaxDepth > MaxBakedGroundDepth)
                    errors.Add($"{family.name}/{label}: maximum supported bounded depth is {MaxBakedGroundDepth}.");
                expectedDepth = band.MaxDepth + 1;
            }

            if (band.Variants.Count == 0)
                errors.Add($"{family.name}/{label}: sprite list is empty.");
            if (band.Variants.Count > MaxVariants)
                errors.Add($"{family.name}/{label}: exceeds {MaxVariants} variants.");
            float totalWeight = 0f;
            int firstValidVariant = -1;
            for (int variantIndex = 0; variantIndex < band.Variants.Count; variantIndex++)
            {
                DungeonWeightedSpriteVariant variant = band.Variants[variantIndex];
                Sprite sprite = variant.Sprite;
                if (variant.Weight < 0f)
                    errors.Add($"{family.name}/{label}: {sprite?.name ?? $"variant {variantIndex}"} has a negative weight.");
                if (!ValidateSprite(sprite, atlas, family.name, label, errors))
                    continue;
                if (firstValidVariant < 0)
                    firstValidVariant = variantIndex;
                totalWeight += Mathf.Max(0f, variant.Weight);
                Rect rect = sprite.rect;
                lookup.SetPixel(variantIndex + 1, startRow + 1 + bandIndex * 2,
                    new Color(rect.x / atlas.width, rect.y / atlas.height,
                        rect.width / atlas.width, rect.height / atlas.height));
            }
            int rectRow = startRow + 1 + bandIndex * 2;
            int choiceRow = rectRow + 1;
            lookup.SetPixel(0, rectRow, new Color(band.Variants.Count, totalWeight, 0, 0));
            if (totalWeight <= 0f && firstValidVariant >= 0)
                Debug.LogWarning($"{family.name}/{label}: all weights are zero; using the first valid Sprite as fallback.", family);
            for (int choice = 0; choice < WeightedChoiceSlots; choice++)
            {
                int selected = totalWeight > 0f
                    ? SelectWeightedVariant(band.Variants, ((choice + 0.5f) / WeightedChoiceSlots) * totalWeight)
                    : Mathf.Max(0, firstValidVariant);
                lookup.SetPixel(choice, choiceRow, new Color(selected, 0, 0, 0));
            }

            int lastDepth = band.Unbounded ? MaxBakedGroundDepth : Mathf.Min(band.MaxDepth, MaxBakedGroundDepth);
            for (int depth = Mathf.Max(0, band.MinDepth); depth <= lastDepth; depth++)
                lookup.SetPixel(depth, startRow, new Color(bandIndex, 0, 0, 0));
        }

        if (fallbackIndex < 0)
            errors.Add($"{family.name}: requires one unbounded fallback band.");
        else
            lookup.SetPixel(MaxBakedGroundDepth + 1, startRow, new Color(fallbackIndex, 0, 0, 0));
    }

    static int SelectWeightedVariant(IReadOnlyList<DungeonWeightedSpriteVariant> variants, float weightedValue)
    {
        float cumulative = 0f;
        int lastPositive = 0;
        for (int i = 0; i < variants.Count; i++)
        {
            float weight = Mathf.Max(0f, variants[i].Weight);
            if (weight <= 0f)
                continue;
            lastPositive = i;
            cumulative += weight;
            if (weightedValue < cumulative)
                return i;
        }
        return lastPositive;
    }

    public static string ComputeGroundFamilyHash(DungeonGroundSurfaceFamily family)
    {
        var content = new StringBuilder(family.StableId);
        foreach (DungeonGroundLayerBand band in family.Bands)
        {
            content.Append('|').Append(band.DisplayName).Append(':').Append(band.MinDepth)
                .Append(':').Append(band.MaxDepth).Append(':').Append(band.Unbounded);
            foreach (DungeonWeightedSpriteVariant variant in band.Variants)
            {
                string guid = "missing";
                long localId = 0;
                if (variant.Sprite != null)
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(variant.Sprite, out guid, out localId);
                content.Append(';').Append(guid).Append(':').Append(localId).Append(':')
                    .Append(variant.Weight.ToString("R", CultureInfo.InvariantCulture));
            }
        }
        return Hash128.Compute(content.ToString()).ToString();
    }

    public static string ComputeSurfaceFamilyHash(DungeonSurfaceFamily family)
    {
        var content = new StringBuilder(family.StableId);
        for (int roleIndex = 0; roleIndex < 4; roleIndex++)
        {
            content.Append('|').Append(roleIndex);
            foreach (DungeonWeightedSpriteVariant variant in family.GetVariants((DungeonSurfaceRole)roleIndex))
            {
                string guid = "missing";
                long localId = 0;
                if (variant.Sprite != null)
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(variant.Sprite, out guid, out localId);
                content.Append(';').Append(guid).Append(':').Append(localId).Append(':')
                    .Append(variant.Weight.ToString("R", CultureInfo.InvariantCulture));
            }
        }
        return Hash128.Compute(content.ToString()).ToString();
    }

    static bool ValidateSprite(Sprite sprite, Texture2D atlas, DungeonSurfaceFamily family,
        DungeonSurfaceRole role, ICollection<string> errors)
    {
        if (sprite == null)
        {
            errors.Add($"{family.name}: {role} contains a missing Sprite reference.");
            return false;
        }
        if (sprite.texture != atlas)
        {
            errors.Add($"{family.name}: {sprite.name} is not sliced from {AtlasPath}.");
            return false;
        }
        if (sprite.rect.width != SpriteSize || sprite.rect.height != SpriteSize)
        {
            errors.Add($"{family.name}: {sprite.name} must be {SpriteSize}x{SpriteSize}, was {sprite.rect.size}.");
            return false;
        }
        return true;
    }

    static bool ValidateSprite(Sprite sprite, Texture2D atlas, string familyName,
        string bandName, ICollection<string> errors)
    {
        if (sprite == null)
        {
            errors.Add($"{familyName}/{bandName}: contains a missing Sprite reference.");
            return false;
        }
        if (sprite.texture != atlas)
        {
            errors.Add($"{familyName}/{bandName}: {sprite.name} is not sliced from {AtlasPath}.");
            return false;
        }
        if (sprite.rect.width != SpriteSize || sprite.rect.height != SpriteSize)
        {
            errors.Add($"{familyName}/{bandName}: {sprite.name} must be {SpriteSize}x{SpriteSize}, was {sprite.rect.size}.");
            return false;
        }
        return true;
    }
}

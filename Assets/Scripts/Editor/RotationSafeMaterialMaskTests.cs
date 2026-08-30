using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RotationSafeMaterialMaskTests
{
    const string BaseAtlasPath = "Assets/Materials/DungeonAtlas.png";
    const string MaskAtlasPath =
        "Assets/Materials/DungeonAtlas_Mask.png";
    const string MaterialPath =
        "Assets/Assets/DungeonTiles/RotationSafeTileAtlas.mat";
    const string ShaderPath = "Assets/Shaders/RotationSafeTileAtlas.shader";

    [Test]
    public void RotationSafeShader_ImportsWithoutShaderErrors()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        Assert.That(shader, Is.Not.Null);
        Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
    }

    [Test]
    public void MaskAtlas_MatchesBaseAtlasAndPixelImportContract()
    {
        Texture2D baseAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
            BaseAtlasPath);
        Texture2D maskAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
            MaskAtlasPath);
        Assert.That(baseAtlas, Is.Not.Null);
        Assert.That(maskAtlas, Is.Not.Null);
        Assert.That(maskAtlas.width, Is.EqualTo(baseAtlas.width));
        Assert.That(maskAtlas.height, Is.EqualTo(baseAtlas.height));

        var importer = AssetImporter.GetAtPath(MaskAtlasPath) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.sRGBTexture, Is.False);
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.textureCompression,
            Is.EqualTo(TextureImporterCompression.Uncompressed));
    }

    [Test]
    public void SharedMaterial_AssignsMaskAndShaderDefaultsRemainCompatible()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Texture2D maskAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
            MaskAtlasPath);
        Assert.That(material, Is.Not.Null);
        Assert.That(material.HasProperty("_MaterialMaskAtlas"), Is.True);
        Assert.That(material.HasProperty("_EmissionIntensity"), Is.True);
        Assert.That(material.HasProperty("_SpecularStrength"), Is.True);
        Assert.That(material.GetTexture("_MaterialMaskAtlas"),
            Is.SameAs(maskAtlas));

        var defaults = new Material(material.shader);
        try
        {
            Assert.That(defaults.GetFloat("_EnableMaterialMask"), Is.Zero);
            Assert.That(defaults.GetFloat("_EmissionIntensity"), Is.Zero);
            Assert.That(defaults.GetFloat("_SpecularStrength"), Is.Zero);
            Assert.That(defaults.GetFloat("_SpecularSteps"), Is.EqualTo(4));
        }
        finally
        {
            Object.DestroyImmediate(defaults);
        }
    }

    [Test]
    public void RepresentativeMaskTile_ContainsAllMaterialRegions()
    {
        byte[] png = File.ReadAllBytes(MaskAtlasPath);
        var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        try
        {
            Assert.That(readable.LoadImage(png, false), Is.True);
            Assert.That(ContainsMask(readable, 1f, 1f, 0f), Is.True);
            Assert.That(ContainsMask(readable, 0f, 83f / 255f, 0f), Is.True);
            Assert.That(ContainsMask(readable, 0f, 1f, 1f), Is.True);
            Assert.That(ContainsMask(readable, 0f, 1f, 0f), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(readable);
        }
    }

    static bool ContainsMask(
        Texture2D texture,
        float emission,
        float roughness,
        float metallic)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            if (Mathf.Abs(pixel.r - emission) <= 0.01f &&
                Mathf.Abs(pixel.g - roughness) <= 0.01f &&
                Mathf.Abs(pixel.b - metallic) <= 0.01f)
                return true;
        }
        return false;
    }
}

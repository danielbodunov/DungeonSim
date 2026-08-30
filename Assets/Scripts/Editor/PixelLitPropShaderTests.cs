using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PixelLitPropShaderTests
{
    const string ShaderPath = "Assets/Shaders/PixelLitProp.shader";
    const string MaterialPath = "Assets/Materials/PixelLitProp.mat";

    [Test]
    public void Shader_ImportsAndConsumesSharedPixelLitCore()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        Assert.That(shader, Is.Not.Null);
        Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);

        string source = File.ReadAllText(ShaderPath);
        StringAssert.Contains(
            "#include \"Assets/Shaders/Includes/DungeonPixelLitCore.hlsl\"",
            source);
        StringAssert.Contains("DungeonPixelLitEvaluate", source);
        StringAssert.Contains("input.uv", source);
    }

    [Test]
    public void Shader_StaysIndependentFromTerrainAndTrapSemantics()
    {
        string source = File.ReadAllText(ShaderPath);
        StringAssert.DoesNotContain("ResolveAtlasRect", source);
        StringAssert.DoesNotContain("_SurfaceLookup", source);
        StringAssert.DoesNotContain("_GroundSurfaceLookup", source);
        StringAssert.DoesNotContain("TrapAttachment", source);
    }

    [Test]
    public void SharedMaterial_ExposesExpectedPropControls()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("DungeonSim/Pixel Lit Prop"));
        Assert.That(material.HasProperty("_BaseMap"), Is.True);
        Assert.That(material.HasProperty("_BaseColor"), Is.True);
        Assert.That(material.HasProperty("_MaterialMask"), Is.True);
        Assert.That(material.HasProperty("_EmissionIntensity"), Is.True);
        Assert.That(material.HasProperty("_SpecularStrength"), Is.True);
        Assert.That(material.HasProperty("_AlphaClip"), Is.True);
        Assert.That(material.HasProperty("_Cutoff"), Is.True);
        Assert.That(material.GetFloat("_EnableMaterialMask"), Is.Zero);
        Assert.That(material.GetFloat("_EmissionIntensity"), Is.Zero);
        Assert.That(material.GetFloat("_SpecularStrength"), Is.Zero);
        Assert.That(material.GetFloat("_AlphaClip"), Is.Zero);
    }

    [Test]
    public void Shader_ClipsBaseAlphaInColorShadowAndDepthPasses()
    {
        string source = File.ReadAllText(ShaderPath);
        Assert.That(CountOccurrences(source, "clip("), Is.EqualTo(3));
        StringAssert.Contains("Name \"ShadowCaster\"", source);
        StringAssert.Contains("Name \"DepthOnly\"", source);
    }

    [Test]
    public void DungeonLightReceiver_PreservesPixelLitPropMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var root = new GameObject("Pixel Lit Receiver Test");
        var child = new GameObject("Renderer");
        child.transform.SetParent(root.transform);
        var renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        try
        {
            DungeonLightReceiver receiver =
                root.AddComponent<DungeonLightReceiver>();
            receiver.RefreshRenderers();
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index,
                   System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}

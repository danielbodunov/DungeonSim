using System.IO;
using NUnit.Framework;

public sealed class DungeonPixelLitCoreTests
{
    const string CorePath =
        "Assets/Shaders/Includes/DungeonPixelLitCore.hlsl";
    const string TerrainShaderPath =
        "Assets/Shaders/RotationSafeTileAtlas.shader";

    [Test]
    public void SharedCore_ExposesUvIndependentLightingInterface()
    {
        string core = File.ReadAllText(CorePath);
        StringAssert.Contains("struct DungeonPixelLitSettings", core);
        StringAssert.Contains("DungeonPixelLitSampleLocalLighting", core);
        StringAssert.Contains("DungeonPixelLitEvaluate", core);
        StringAssert.DoesNotContain("ResolveAtlasRect", core);
        StringAssert.DoesNotContain("_SurfaceLookup", core);
        StringAssert.DoesNotContain("_GroundSurfaceLookup", core);
        StringAssert.DoesNotContain("surfaceSlot", core);
    }

    [Test]
    public void TerrainShader_ConsumesCoreWithoutDuplicatingLightingEvaluation()
    {
        string shader = File.ReadAllText(TerrainShaderPath);
        StringAssert.Contains(
            "#include \"Assets/Shaders/Includes/DungeonPixelLitCore.hlsl\"",
            shader);
        StringAssert.Contains("DungeonPixelLitEvaluate", shader);
        StringAssert.Contains("ResolveAtlasRect", shader);
        StringAssert.DoesNotContain("SampleDungeonLocalLighting", shader);
        StringAssert.DoesNotContain("smoothSpecular =", shader);
    }
}

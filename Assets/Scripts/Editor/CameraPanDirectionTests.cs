#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public sealed class CameraPanDirectionTests
{
    [TestCase(8f, 0f, -0.16f, 0f)]
    [TestCase(-8f, 0f, 0.16f, 0f)]
    [TestCase(0f, 8f, 0f, -0.16f)]
    [TestCase(0f, -8f, 0f, 0.16f)]
    public void DefaultMiddleDragMovesCameraOppositeMouseMotion(
        float mouseX,
        float mouseY,
        float expectedX,
        float expectedY)
    {
        Vector3 delta = CameraFollow.CalculateMiddleMousePanDelta(
            new Vector2(mouseX, mouseY), 0.02f, false);

        Assert.That(delta.x, Is.EqualTo(expectedX).Within(0.0001f));
        Assert.That(delta.y, Is.EqualTo(expectedY).Within(0.0001f));
        Assert.That(delta.z, Is.Zero);
    }

    [Test]
    public void InvertOverrideReversesBothDragAxesWithoutChangingMagnitude()
    {
        var mouse = new Vector2(6f, -4f);
        Vector3 grab = CameraFollow.CalculateMiddleMousePanDelta(
            mouse, 0.02f, false);
        Vector3 inverted = CameraFollow.CalculateMiddleMousePanDelta(
            mouse, 0.02f, true);

        Assert.That(inverted, Is.EqualTo(-grab));
        Assert.That(inverted.magnitude,
            Is.EqualTo(grab.magnitude).Within(0.0001f));
    }
}
#endif

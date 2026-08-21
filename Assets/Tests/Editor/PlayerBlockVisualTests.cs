using ActionRPG.Player;
using NUnit.Framework;
using UnityEngine;

public class PlayerBlockVisualTests
{
    [Test]
    public void BuildArcPointsStartsAndEndsAtRequestedBlockAngle()
    {
        Vector3[] points = PlayerBlockVisual.BuildArcPoints(
            Vector3.zero,
            Vector3.forward,
            2f,
            140f,
            0.1f,
            8
        );

        Assert.That(points.Length, Is.EqualTo(9));
        Assert.That(Vector3.Distance(points[0], points[points.Length - 1]), Is.GreaterThan(0.1f));
        Assert.That(points[0].y, Is.EqualTo(0.1f));
        Assert.That(points[points.Length - 1].y, Is.EqualTo(0.1f));
    }

    [Test]
    public void BuildArcPointsFacesForwardAroundThePlayer()
    {
        Vector3[] points = PlayerBlockVisual.BuildArcPoints(
            new Vector3(3f, 0f, -2f),
            Vector3.right,
            1.5f,
            90f,
            0.2f,
            4
        );

        Vector3 midpoint = points[2] - new Vector3(3f, 0.2f, -2f);
        Assert.That(Vector3.Angle(Vector3.right, midpoint), Is.LessThan(1f));
        Assert.That(midpoint.magnitude, Is.EqualTo(1.5f).Within(0.001f));
    }
}

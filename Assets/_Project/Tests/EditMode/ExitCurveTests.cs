using NUnit.Framework;
using UnityEngine;

public class ExitCurveTests
{
    private const float Epsilon = 1e-4f;

    [Test]
    public void Evaluate_MidParameter_MatchesClosedForm()
    {
        var curve = new CubicBezier(
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f));

        Vector2 atHalf = curve.Evaluate(0.5f);
        Assert.That(atHalf.x, Is.EqualTo(0.5f).Within(Epsilon));
        Assert.That(atHalf.y, Is.EqualTo(0.75f).Within(Epsilon));
    }

    [Test]
    public void Evaluate_QuarterParameter_MatchesClosedForm()
    {
        var curve = new CubicBezier(
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f));

        Vector2 atQuarter = curve.Evaluate(0.25f);
        Assert.That(atQuarter.x, Is.EqualTo(0.15625f).Within(Epsilon));
        Assert.That(atQuarter.y, Is.EqualTo(0.5625f).Within(Epsilon));
    }

    [Test]
    public void Evaluate_Endpoints_ReturnsControlPoints()
    {
        var curve = new CubicBezier(
            new Vector2(1f, 2f),
            new Vector2(3f, 4f),
            new Vector2(5f, 6f),
            new Vector2(7f, 8f));

        Assert.That(curve.Evaluate(0f), Is.EqualTo(new Vector2(1f, 2f)).Within(Epsilon));
        Assert.That(curve.Evaluate(1f), Is.EqualTo(new Vector2(7f, 8f)).Within(Epsilon));
    }

    [Test]
    public void Sample_FiveSegments_ReturnsEvenlySpacedPointsIncludingEndpoints()
    {
        var curve = new CubicBezier(
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f));

        Vector2[] samples = curve.Sample(4);

        Assert.That(samples.Length, Is.EqualTo(5));
        Assert.That(samples[0], Is.EqualTo(curve.Evaluate(0f)).Within(Epsilon));
        Assert.That(samples[1], Is.EqualTo(curve.Evaluate(0.25f)).Within(Epsilon));
        Assert.That(samples[2], Is.EqualTo(curve.Evaluate(0.5f)).Within(Epsilon));
        Assert.That(samples[3], Is.EqualTo(curve.Evaluate(0.75f)).Within(Epsilon));
        Assert.That(samples[4], Is.EqualTo(curve.Evaluate(1f)).Within(Epsilon));
    }

    [Test]
    public void GetTangent_Start_IsParallelToFirstSegment()
    {
        var curve = new CubicBezier(
            new Vector2(0f, 0f),
            new Vector2(0f, 2f),
            new Vector2(2f, 2f),
            new Vector2(2f, 0f));

        Vector2 tangent = curve.GetTangent(0f);
        Assert.That(tangent.x, Is.EqualTo(0f).Within(Epsilon));
        Assert.That(tangent.y, Is.GreaterThan(0f));
    }

    [Test]
    public void DefaultCurve_StartsAtStartAndEndsAtEnd()
    {
        var curve = ExitCurveFactory.DefaultCurve(
            new Vector2(0f, 0f),
            new Vector2(5f, 3f));

        Assert.That(curve.Evaluate(0f), Is.EqualTo(new Vector2(0f, 0f)).Within(Epsilon));
        Assert.That(curve.Evaluate(1f), Is.EqualTo(new Vector2(5f, 3f)).Within(Epsilon));
    }

    [Test]
    public void DefaultCurve_DepartsAlongTravelDirection()
    {
        Vector2 start = new Vector2(0f, 0f);
        Vector2 end = new Vector2(5f, 3f);
        Vector2 dir = (end - start).normalized;
        var curve = ExitCurveFactory.DefaultCurve(start, end);

        Vector2 tangent = curve.GetTangent(0f);
        Assert.That(Vector2.Angle(tangent, dir), Is.LessThan(1f));
    }

    [Test]
    public void DefaultCurve_ApproachesEndAlongTravelDirection()
    {
        Vector2 start = new Vector2(0f, 0f);
        Vector2 end = new Vector2(5f, 3f);
        Vector2 dir = (end - start).normalized;
        var curve = ExitCurveFactory.DefaultCurve(start, end);

        Vector2 tangent = curve.GetTangent(1f);
        Assert.That(Vector2.Angle(tangent, dir), Is.LessThan(1f));
    }

    [Test]
    public void FromLevelData_NoExitCurve_ReturnsDefaultCurve()
    {
        var level = new LevelData { exitCurve = null };
        Vector2 start = new Vector2(7f, 3f);
        Vector2 end = new Vector2(11f, 3f);

        var curve = ExitCurveFactory.FromLevelData(level, start, end);

        Assert.That(curve.Evaluate(0f), Is.EqualTo(start).Within(Epsilon));
        Assert.That(curve.Evaluate(1f), Is.EqualTo(end).Within(Epsilon));
    }

    [Test]
    public void FromLevelData_TwoPointsOnly_TreatsAsMissing()
    {
        var level = new LevelData
        {
            exitCurve = new[]
            {
                new Vector2Int(7, 3),
                new Vector2Int(9, 3)
            }
        };

        var curve = ExitCurveFactory.FromLevelData(level, new Vector2(7f, 3f), new Vector2(11f, 3f));

        Assert.That(curve.Evaluate(1f), Is.EqualTo(new Vector2(11f, 3f)).Within(Epsilon));
    }

    [Test]
    public void FromLevelData_FourPoints_UsesThemExactly()
    {
        var level = new LevelData
        {
            exitCurve = new[]
            {
                new Vector2Int(7, 3),
                new Vector2Int(8, 3),
                new Vector2Int(9, 4),
                new Vector2Int(11, 3)
            }
        };

        var expected = new CubicBezier(
            new Vector2(7f, 3f),
            new Vector2(8f, 3f),
            new Vector2(9f, 4f),
            new Vector2(11f, 3f));

        var curve = ExitCurveFactory.FromLevelData(level, new Vector2(7f, 3f), new Vector2(11f, 3f));

        Assert.That(curve.Evaluate(0f), Is.EqualTo(expected.Evaluate(0f)).Within(Epsilon));
        Assert.That(curve.Evaluate(0.5f), Is.EqualTo(expected.Evaluate(0.5f)).Within(Epsilon));
        Assert.That(curve.Evaluate(1f), Is.EqualTo(expected.Evaluate(1f)).Within(Epsilon));
    }

    [Test]
    public void DefaultCurve_ShortSpan_DoesNotCrossControlPoints()
    {
        var curve = ExitCurveFactory.DefaultCurve(
            new Vector2(0f, 0f),
            new Vector2(1.2f, 0f));

        Vector2 mid = curve.Evaluate(0.5f);
        Assert.That(mid.x, Is.GreaterThan(0f));
        Assert.That(mid.x, Is.LessThan(1.2f));
        Assert.That(curve.Evaluate(0f), Is.EqualTo(Vector2.zero).Within(Epsilon));
        Assert.That(curve.Evaluate(1f), Is.EqualTo(new Vector2(1.2f, 0f)).Within(Epsilon));
    }

    [Test]
    public void DefaultCurve_ZeroLengthSpan_FallsBackToRightDirection()
    {
        var curve = ExitCurveFactory.DefaultCurve(Vector2.zero, Vector2.zero);

        Vector2 tangent = curve.GetTangent(0f);
        Assert.IsFalse(float.IsNaN(tangent.x));
        Assert.IsFalse(float.IsNaN(tangent.y));
        Assert.That(tangent.x, Is.GreaterThan(0f));
        Assert.That(curve.Evaluate(0f), Is.EqualTo(Vector2.zero).Within(Epsilon));
        Assert.That(curve.Evaluate(1f), Is.EqualTo(Vector2.zero).Within(Epsilon));
    }

    [Test]
    public void LevelData_JsonWithExitCurve_ParsesControlPoints()
    {
        const string json = @"{
            ""id"": 1,
            ""exitCurve"": [
                {""x"": 7, ""y"": 3},
                {""x"": 8, ""y"": 3},
                {""x"": 9, ""y"": 4},
                {""x"": 11, ""y"": 3}
            ]
        }";

        var level = JsonUtility.FromJson<LevelData>(json);

        Assert.That(level.exitCurve, Is.Not.Null);
        Assert.That(level.exitCurve.Length, Is.EqualTo(4));
        Assert.That(level.exitCurve[0], Is.EqualTo(new Vector2Int(7, 3)));
        Assert.That(level.exitCurve[3], Is.EqualTo(new Vector2Int(11, 3)));
    }

    [Test]
    public void LevelData_JsonWithoutExitCurve_LeavesItNull()
    {
        const string json = @"{
            ""id"": 1
        }";

        var level = JsonUtility.FromJson<LevelData>(json);

        Assert.IsNull(level.exitCurve);
    }
}
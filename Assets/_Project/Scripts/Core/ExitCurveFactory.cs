using UnityEngine;

public static class ExitCurveFactory
{
    private const float StraightLength = 1f;
    private const float MinStraightSpanRatio = 0.35f;

    public static CubicBezier FromLevelData(LevelData level, Vector2 start, Vector2 end)
    {
        if (level != null && level.exitCurve != null && level.exitCurve.Length == 4)
        {
            return new CubicBezier(
                level.exitCurve[0],
                level.exitCurve[1],
                level.exitCurve[2],
                level.exitCurve[3]);
        }
        if (level != null && level.exitCurve != null && level.exitCurve.Length != 4)
        {
            Debug.LogWarning(
                $"[ExitCurveFactory] Level {level.id} defines exitCurve with {level.exitCurve.Length} points; " +
                "exactly 4 are required — falling back to the default curve.");
        }
        return DefaultCurve(start, end);
    }

    public static CubicBezier DefaultCurve(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;
        float span = dir.magnitude;
        if (span < 1e-6f)
        {
            dir = Vector2.right;
            span = 2f * StraightLength;
        }
        else
        {
            dir /= span;
        }

        float leg = Mathf.Min(StraightLength, span * MinStraightSpanRatio);

        var p0 = start;
        var p1 = start + dir * leg;
        var p2 = end - dir * leg;
        var p3 = end;
        return new CubicBezier(p0, p1, p2, p3);
    }
}
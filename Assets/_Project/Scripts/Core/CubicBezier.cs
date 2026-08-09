using UnityEngine;

public class CubicBezier
{
    private readonly Vector2 _p0;
    private readonly Vector2 _p1;
    private readonly Vector2 _p2;
    private readonly Vector2 _p3;

    public CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        _p0 = p0;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;
    }

    public Vector2 Evaluate(float t)
    {
        float u = 1f - t;
        return u * u * u * _p0
               + 3f * u * u * t * _p1
               + 3f * u * t * t * _p2
               + t * t * t * _p3;
    }

    public Vector2 GetTangent(float t)
    {
        float u = 1f - t;
        return 3f * u * u * (_p1 - _p0)
               + 6f * u * t * (_p2 - _p1)
               + 3f * t * t * (_p3 - _p2);
    }

    public Vector2[] Sample(int segments)
    {
        var points = new Vector2[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            points[i] = Evaluate((float)i / segments);
        }
        return points;
    }
}
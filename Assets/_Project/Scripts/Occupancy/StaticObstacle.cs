using UnityEngine;

public class StaticObstacle : IOccupant
{
    private readonly Vector3Int _position;

    public Vector3Int[] OccupiedTiles => new[] { _position };

    public StaticObstacle(Vector3Int position)
    {
        _position = position;
    }
}
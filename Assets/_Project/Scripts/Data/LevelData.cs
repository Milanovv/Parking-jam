using System;
using UnityEngine;

[Serializable]
public class LevelData
{
    public int id;
    public string name;
    public int gridWidth;
    public int gridHeight;
    public int moveLimit;
    public int timeLimit;
    public int levelUndos = 3;
    public Vector2Int[] exitTiles;
    public VehicleData[] vehicles;
    public StaticObstacleData[] staticObstacles = Array.Empty<StaticObstacleData>();
    public PedestrianData[] pedestrians = Array.Empty<PedestrianData>();
    public BarrierData[] barriers = Array.Empty<BarrierData>();
    public Vector2Int[] exitCurve;
}

[Serializable]
public class VehicleData
{
    public string id;
    public Vector2Int[] tiles;
    public string orientation;
}

[Serializable]
public class StaticObstacleData
{
    public Vector2Int tile;
}

[Serializable]
public class PedestrianData
{
    public Vector2Int[] route;
}

[Serializable]
public class BarrierData
{
    public string miniGameScene;
    public Vector2Int tile;
}
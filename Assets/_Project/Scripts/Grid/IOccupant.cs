using UnityEngine;

public interface IOccupant
{
    Vector3Int[] OccupiedTiles { get; }

    bool CausesCollision => true;
}

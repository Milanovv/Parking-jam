using UnityEngine;

public class Barrier : IOccupant
{
    private readonly Vector3Int _position;
    private bool _locked;

    public bool Locked => _locked;

    public Vector3Int[] OccupiedTiles => new[] { _position };

    public bool CausesCollision => !_locked;

    public Barrier(Vector3Int position)
    {
        _position = position;
        _locked = true;
    }

    public void Unlock()
    {
        _locked = false;
    }
}
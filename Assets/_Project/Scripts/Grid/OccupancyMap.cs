using System.Collections.Generic;
using UnityEngine;

public class OccupancyMap
{
    private readonly Dictionary<Vector3Int, IOccupant> _map = new();

    public bool IsTileFree(Vector3Int tile)
    {
        return !_map.ContainsKey(tile);
    }

    public void Place(IOccupant occupant)
    {
        foreach (var tile in occupant.OccupiedTiles)
            _map[tile] = occupant;
    }

    public void Remove(IOccupant occupant)
    {
        foreach (var tile in occupant.OccupiedTiles)
            _map.Remove(tile);
    }

    public bool TryGetOccupant(Vector3Int tile, out IOccupant occupant)
    {
        return _map.TryGetValue(tile, out occupant);
    }

    public void Clear()
    {
        _map.Clear();
    }
}

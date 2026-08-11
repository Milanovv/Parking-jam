using UnityEngine;

public class Pedestrian : IOccupant
{
    private readonly Vector3Int[] _route;
    private int _index;
    private int _direction = 1;
    private bool _turning;

    public Vector3Int[] OccupiedTiles => new[] { _route[_index] };

    public int RouteIndex => _index;

    public Pedestrian(Vector3Int[] route)
    {
        _route = route;
    }

    public void Reset()
    {
        _index = 0;
        _direction = 1;
        _turning = false;
    }

    public bool Advance(OccupancyMap map)
    {
        if (_turning)
            _turning = false;

        int next = _index + _direction;
        if (next < 0 || next >= _route.Length)
        {
            _direction = -_direction;
            if (!_turning)
            {
                _turning = true;
                return false;
            }
            next = _index + _direction;
        }
        if (next < 0 || next >= _route.Length) return false;

        var target = _route[next];
        if (!map.IsTileFree(target)) return false;

        map.Remove(this);
        _index = next;
        map.Place(this);
        return true;
    }
}
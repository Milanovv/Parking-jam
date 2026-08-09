using System.Collections;
using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
    [SerializeField] private Vehicle _vehicle;
    [SerializeField] private float _snapDuration = 0.15f;

    private GridController _gridController;
    private bool _isAnimating;

    public bool IsAnimating => _isAnimating;

    public void Initialize(GridController gridController)
    {
        _gridController = gridController;
        if (_vehicle == null) _vehicle = GetComponent<Vehicle>();
    }

    public bool TryMoveDirection(Vector3Int direction, OccupancyMap occupancyMap)
    {
        if (_isAnimating || _vehicle == null) return false;

        int steps = SweepSteps(direction, occupancyMap);
        if (steps <= 0) return false;

        Vector3Int destination = _vehicle.GridPosition + direction * steps;
        Vector3Int[] oldTiles = _vehicle.OccupiedTiles;

        occupancyMap.Remove(_vehicle);
        _vehicle.MoveTo(destination);
        occupancyMap.Place(_vehicle);

        Vector3 worldTarget = _gridController.Grid.CellToWorld((Vector3Int)_vehicle.GridPosition);
        StartCoroutine(SnapAnimation(worldTarget));

        return true;
    }

    private int SweepSteps(Vector3Int direction, OccupancyMap occupancyMap)
    {
        bool horizontal = _vehicle.Orientation == Orientation.Horizontal;
        int dir = horizontal ? direction.x : direction.y;
        if (dir == 0) return 0;

        int axis = horizontal ? _vehicle.GridPosition.x : _vehicle.GridPosition.y;
        int cross = horizontal ? _vehicle.GridPosition.y : _vehicle.GridPosition.x;
        int length = _vehicle.OccupiedTiles.Length;
        int limit = horizontal ? _gridController.GridWidth : _gridController.GridHeight;

        int steps = 0;
        while (true)
        {
            int leading = dir > 0 ? axis + length - 1 + steps + 1 : axis - (steps + 1);
            if (leading < 0 || leading >= limit) break;

            var tile = horizontal
                ? new Vector3Int(leading, cross, 0)
                : new Vector3Int(cross, leading, 0);
            if (!occupancyMap.IsTileFree(tile)) break;

            steps++;
        }

        return steps;
    }

    private IEnumerator SnapAnimation(Vector3 target)
    {
        _isAnimating = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < _snapDuration)
        {
            float t = elapsed / _snapDuration;
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        _isAnimating = false;
    }
}

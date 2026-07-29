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
        int steps = 0;
        Vector3Int current = _vehicle.GridPosition;
        int length = _vehicle.OccupiedTiles.Length;

        while (true)
        {
            Vector3Int nextTile = current + direction + direction * steps;

            if (_vehicle.Orientation == Orientation.Horizontal)
            {
                int xMin = nextTile.x;
                int xMax = nextTile.x + length - 1;
                if (xMin < 0 || xMax >= _gridController.GridWidth) break;
            }
            else
            {
                int yMin = nextTile.y;
                int yMax = nextTile.y + length - 1;
                if (yMin < 0 || yMax >= _gridController.GridHeight) break;
            }

            bool blocked = false;
            for (int i = 0; i < length; i++)
            {
                Vector3Int checkTile;
                if (_vehicle.Orientation == Orientation.Horizontal)
                    checkTile = new Vector3Int(nextTile.x + i, nextTile.y, 0);
                else
                    checkTile = new Vector3Int(nextTile.x, nextTile.y + i, 0);

                if (!occupancyMap.IsTileFree(checkTile))
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked) break;
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

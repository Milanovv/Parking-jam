using System.Collections;
using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
    [SerializeField] private Vehicle _vehicle;
    [SerializeField] private float _snapDuration = 0.15f;

    private const float ExitDriveSpeed = 8f;
    private const float ExitLaneLength = 8f;

    private GridController _gridController;
    private bool _isAnimating;

    public bool IsAnimating => _isAnimating;

    public void StopAnimation()
    {
        StopAllCoroutines();
        _isAnimating = false;
        transform.position = _gridController.Grid.CellToWorld(_vehicle.GridPosition);
    }

    public void Initialize(GridController gridController)
    {
        _gridController = gridController;
        if (_vehicle == null) _vehicle = GetComponent<Vehicle>();
    }

    public bool TryMoveDirection(Vector3Int direction, OccupancyMap occupancyMap)
    {
        if (_isAnimating || _vehicle == null) return false;

        var gameManager = GameManager.Instance;
        if (gameManager == null) return false;

        MoveOutcome outcome = gameManager.ResolveMove(_vehicle, direction);
        if (outcome == null) return false;

        if (outcome.Kind == MoveOutcomeKind.Exited)
        {
            gameManager.UnregisterVehicleOnMap(_vehicle);
            StartCoroutine(ExitSequence(outcome.Destination, direction, gameManager));
            return true;
        }

        if (outcome.Kind != MoveOutcomeKind.Completed || outcome.Steps <= 0) return false;

        Vector3Int destination = outcome.Destination;
        occupancyMap.Remove(_vehicle);
        _vehicle.MoveTo(destination);
        occupancyMap.Place(_vehicle);

        Vector3 worldTarget = _gridController.Grid.CellToWorld((Vector3Int)_vehicle.GridPosition);
        StartCoroutine(SnapAnimation(worldTarget, deactivateAtEnd: false));

        return true;
    }

    private IEnumerator SnapAnimation(Vector3 target, bool deactivateAtEnd)
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
        if (deactivateAtEnd) gameObject.SetActive(false);
    }

    private IEnumerator ExitSequence(Vector3Int crossingCell, Vector3Int direction, GameManager gameManager)
    {
        yield return SnapAnimation(_gridController.Grid.CellToWorld(crossingCell), deactivateAtEnd: false);

        _isAnimating = true;
        Vector2 start = new Vector2(crossingCell.x, crossingCell.y);
        Vector2 outward = new Vector2(direction.x, direction.y);
        Vector2 end = start + outward * ExitLaneLength;
        CubicBezier gridCurve = gameManager.BuildExitCurve(start, end);
        CubicBezier anchored = TranslateToStart(gridCurve, start);
        yield return DriveAlong(ToWorldCurve(anchored));

        _isAnimating = false;
        gameManager.CompleteExit();
        gameObject.SetActive(false);
    }

    private static CubicBezier TranslateToStart(CubicBezier curve, Vector2 start)
    {
        Vector2 delta = start - curve.P0;
        return new CubicBezier(
            curve.P0 + delta,
            curve.P1 + delta,
            curve.P2 + delta,
            curve.P3 + delta);
    }

    private CubicBezier ToWorldCurve(CubicBezier curve)
    {
        return new CubicBezier(
            WorldFromGrid(curve.P0),
            WorldFromGrid(curve.P1),
            WorldFromGrid(curve.P2),
            WorldFromGrid(curve.P3));
    }

    private Vector2 WorldFromGrid(Vector2 grid)
    {
        var cell = new Vector3Int(Mathf.RoundToInt(grid.x), Mathf.RoundToInt(grid.y), 0);
        return (Vector2)_gridController.Grid.CellToWorld(cell);
    }

    private IEnumerator DriveAlong(CubicBezier curve)
    {
        Vector2[] samples = curve.Sample(32);
        float length = 0f;
        for (int i = 1; i < samples.Length; i++)
            length += Vector2.Distance(samples[i - 1], samples[i]);
        float rate = ExitDriveSpeed / Mathf.Max(length, 0.01f);

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime * rate);
            Vector2 p = curve.Evaluate(t);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            yield return null;
        }
    }
}

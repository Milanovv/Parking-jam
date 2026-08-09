using System.Collections;
using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
    [SerializeField] private Vehicle _vehicle;
    [SerializeField] private float _snapDuration = 0.15f;

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
            Vector3 exitTarget = _gridController.Grid.CellToWorld(outcome.Destination);
            StartCoroutine(SnapAnimation(exitTarget, deactivateAtEnd: true));
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
}

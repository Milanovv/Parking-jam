using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private GridController _gridController;

    private InputAction _pressAction;
    private InputAction _pointAction;
    private Camera _camera;
    private Vehicle _selectedVehicle;
    private Vector3Int _dragStartCell;

    private void Awake()
    {
        _camera = Camera.main;

        _pressAction = new InputAction("Press", InputActionType.Button);
        _pressAction.AddBinding("<Mouse>/leftButton", interactions: "Press");
        _pressAction.AddBinding("<Touchscreen>/press", interactions: "Press");

        _pointAction = new InputAction("Point", InputActionType.Value, "<Mouse>/position");
        _pointAction.AddBinding("<Touchscreen>/position");
    }

    private void OnEnable()
    {
        _pressAction.started += OnPressStarted;
        _pressAction.performed += OnPressPerformed;
        _pressAction.Enable();
        _pointAction.Enable();
    }

    private void OnDisable()
    {
        _pressAction.started -= OnPressStarted;
        _pressAction.performed -= OnPressPerformed;
        _pressAction.Disable();
        _pointAction.Disable();
    }

    private void OnPressStarted(InputAction.CallbackContext ctx)
    {
        if (_gridController == null) return;
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        Vector2 screenPos = _pointAction.ReadValue<Vector2>();
        if (!TryHitGridPlane(screenPos, out Vector3 worldPos)) return;
        Vector3Int cell = _gridController.WorldToCell(worldPos);

        if (gameManager.OccupancyMap.TryGetOccupant(cell, out var occupant) && occupant is Vehicle vehicle)
        {
            _selectedVehicle = vehicle;
            _dragStartCell = cell;
            return;
        }

        if (cell == gameManager.BarrierTile)
            gameManager.RequestBarrierUnlock();
    }

    private void OnPressPerformed(InputAction.CallbackContext ctx)
    {
        if (_selectedVehicle == null || _gridController == null) return;
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        Vector2 screenPos = _pointAction.ReadValue<Vector2>();
        if (!TryHitGridPlane(screenPos, out Vector3 worldPos)) return;
        Vector3Int cell = _gridController.WorldToCell(worldPos);
        Vector3Int dragDelta = cell - _dragStartCell;

        Vector3Int direction = Vector3Int.zero;
        if (_selectedVehicle.Orientation == Orientation.Horizontal)
            direction.x = System.Math.Sign(dragDelta.x);
        else
            direction.y = System.Math.Sign(dragDelta.y);

        if (direction != Vector3Int.zero)
        {
            var movement = _selectedVehicle.GetComponent<VehicleMovement>();
            if (movement != null)
                movement.TryMoveDirection(direction, gameManager.OccupancyMap);
        }

        _selectedVehicle = null;
    }

    private bool TryHitGridPlane(Vector2 screenPos, out Vector3 worldPos)
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null)
        {
            worldPos = default;
            return false;
        }

        var ray = _camera.ScreenPointToRay(screenPos);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = default;
        return false;
    }
}
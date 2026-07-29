using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private GridController _gridController;
    [SerializeField] private OccupancyMap _occupancyMap;

    private InputAction _pressAction;
    private InputAction _pointAction;
    private Camera _camera;
    private Vehicle _selectedVehicle;
    private Vector3Int _dragStartCell;

    private void Awake()
    {
        _camera = Camera.main;

        _pressAction = new InputAction("Press", InputActionType.Button, "<Mouse>/leftButton");
        _pressAction.AddBinding("<Touchscreen>/press");
        _pressAction.AddInteraction("Press");

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
        if (_gridController == null || _occupancyMap == null) return;

        Vector2 screenPos = _pointAction.ReadValue<Vector2>();
        Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0;

        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit != null && hit.TryGetComponent<Vehicle>(out var vehicle))
        {
            _selectedVehicle = vehicle;
            _dragStartCell = _gridController.WorldToCell(worldPos);
        }
    }

    private void OnPressPerformed(InputAction.CallbackContext ctx)
    {
        if (_selectedVehicle == null || _gridController == null || _occupancyMap == null) return;

        Vector2 screenPos = _pointAction.ReadValue<Vector2>();
        Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0;

        Vector3Int cellPos = _gridController.WorldToCell(worldPos);
        Vector3Int dragDelta = cellPos - _dragStartCell;

        Vector3Int direction = Vector3Int.zero;
        if (_selectedVehicle.Orientation == Orientation.Horizontal)
            direction.x = System.Math.Sign(dragDelta.x);
        else
            direction.y = System.Math.Sign(dragDelta.y);

        if (direction != Vector3Int.zero)
        {
            var movement = _selectedVehicle.GetComponent<VehicleMovement>();
            if (movement != null)
                movement.TryMoveDirection(direction, _occupancyMap);
        }

        _selectedVehicle = null;
    }
}

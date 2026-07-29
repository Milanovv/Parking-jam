using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GridController _gridController;
    [SerializeField] private float _padding = 2f;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null) _camera = Camera.main;
    }

    private void Start()
    {
        FrameGrid();
    }

    public void FrameGrid()
    {
        if (_gridController == null || _camera == null) return;

        float gridWidth = _gridController.GridWidth;
        float gridHeight = _gridController.GridHeight;

        Vector3 gridCenter = new Vector3(gridWidth * 0.5f, gridHeight * 0.5f, -10f);
        transform.position = gridCenter;

        float maxDimension = Mathf.Max(gridWidth, gridHeight);
        _camera.orthographicSize = (maxDimension * 0.5f) + _padding;
    }
}

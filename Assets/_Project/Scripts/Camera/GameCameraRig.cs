using UnityEngine;

public class GameCameraRig : MonoBehaviour
{
    [SerializeField] private float _pitchDegrees = 40f;
    [SerializeField] private float _framingMargin = 1.25f;

    private void Start()
    {
        var grid = FindFirstObjectByType<GridController>();
        if (grid != null) Frame(grid);
    }

    public void Frame(GridController grid)
    {
        var cam = GetComponent<Camera>();
        cam.orthographic = false;

        Vector3 center = grid.CellToWorld(new Vector3Int(grid.GridWidth / 2, grid.GridHeight / 2, 0));
        float halfExtent = Mathf.Max(grid.GridWidth, grid.GridHeight) * 0.5f * _framingMargin;
        float distance = halfExtent / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

        transform.rotation = Quaternion.Euler(_pitchDegrees, 0f, 0f);
        transform.position = center - transform.forward * distance;
    }
}
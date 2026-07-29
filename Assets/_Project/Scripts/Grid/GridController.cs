using UnityEngine;
using UnityEngine.Tilemaps;

public class GridController : MonoBehaviour
{
    [SerializeField] private int _gridWidth = 5;
    [SerializeField] private int _gridHeight = 5;

    public Grid Grid { get; private set; }
    public Tilemap Tilemap { get; private set; }
    public int GridWidth => _gridWidth;
    public int GridHeight => _gridHeight;

    private void Awake()
    {
        Grid = GetComponent<Grid>();
        Tilemap = GetComponentInChildren<Tilemap>();
    }

    public void SetGridSize(int width, int height)
    {
        _gridWidth = width;
        _gridHeight = height;
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        Vector3 flattened = new Vector3(worldPosition.x, worldPosition.y, 0);
        return (Vector3Int)Grid.WorldToCell(flattened);
    }

    public Vector3 CellToWorld(Vector3Int cellPosition)
    {
        return Grid.CellToWorld(cellPosition);
    }

    public bool IsInBounds(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < _gridWidth &&
               cell.y >= 0 && cell.y < _gridHeight;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Vector3 origin = transform.position;
        Vector3 size = new Vector3(_gridWidth, _gridHeight, 0);
        Gizmos.DrawWireCube(origin + size * 0.5f, size);
    }
}

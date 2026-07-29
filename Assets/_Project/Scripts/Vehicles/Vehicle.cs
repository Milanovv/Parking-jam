using UnityEngine;

public enum Orientation
{
    Horizontal,
    Vertical
}

public class Vehicle : MonoBehaviour, IOccupant
{
    [SerializeField] private string _vehicleId;
    [SerializeField] private Orientation _orientation;

    private Vector3Int[] _occupiedTiles;

    public string VehicleId => _vehicleId;
    public Orientation Orientation => _orientation;
    public Vector3Int GridPosition { get; private set; }

    public Vector3Int[] OccupiedTiles => _occupiedTiles;

    public void Initialize(string id, Orientation orientation, Vector3Int gridPosition, int length)
    {
        _vehicleId = id;
        _orientation = orientation;
        GridPosition = gridPosition;
        UpdateOccupiedTiles(length);
    }

    public void MoveTo(Vector3Int newGridPosition)
    {
        GridPosition = newGridPosition;
        UpdateOccupiedTiles(_occupiedTiles.Length);
    }

    private void UpdateOccupiedTiles(int length)
    {
        _occupiedTiles = new Vector3Int[length];
        for (int i = 0; i < length; i++)
        {
            if (_orientation == Orientation.Horizontal)
                _occupiedTiles[i] = new Vector3Int(GridPosition.x + i, GridPosition.y, 0);
            else
                _occupiedTiles[i] = new Vector3Int(GridPosition.x, GridPosition.y + i, 0);
        }
    }
}

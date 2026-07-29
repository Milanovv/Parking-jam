using UnityEngine;

public enum GameState
{
    Playing,
    Won
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridController _gridController;
    [SerializeField] private InputHandler _inputHandler;

    public static GameManager Instance { get; private set; }

    public OccupancyMap OccupancyMap { get; private set; }
    public GameState State { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        OccupancyMap = new OccupancyMap();
        State = GameState.Playing;
    }

    private void Start()
    {
        PlaceVehiclesOnMap();
    }

    private void PlaceVehiclesOnMap()
    {
        var vehicles = FindObjectsByType<Vehicle>();
        foreach (var vehicle in vehicles)
        {
            OccupancyMap.Place(vehicle);
        }
    }

    public void RegisterVehicleOnMap(Vehicle vehicle)
    {
        OccupancyMap.Place(vehicle);
    }
}

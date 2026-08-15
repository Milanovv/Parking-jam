using System;
using System.Collections.Generic;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    [SerializeField] private int _levelId = 1;
    [SerializeField] private bool _autoStartOnPlay = true;
    [SerializeField] private GridController _gridController;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private LevelSessionStats _sessionStats;
    [SerializeField] private GameCameraRig _cameraRig;
    [SerializeField] private GameObject _carPrefab;
    [SerializeField] private GameObject _truckPrefab;
    [SerializeField] private GameObject _busPrefab;
    [SerializeField] private GameObject _pedestrianPrefab;
    [SerializeField] private GameObject _barrierPrefab;
    [SerializeField] private GameObject _obstaclePrefab;

    private readonly List<Vehicle> _vehicles = new List<Vehicle>();
    private readonly List<PedestrianView> _pedestrianViews = new List<PedestrianView>();
    private readonly List<GameObject> _obstacleViews = new List<GameObject>();
    private GameObject _barrierView;

    public int LevelId => _levelId;
    public bool AutoStartOnPlay
    {
        get => _autoStartOnPlay;
        set => _autoStartOnPlay = value;
    }

    public LevelData CurrentLevel { get; private set; }
    public IReadOnlyList<Vehicle> Vehicles => _vehicles;
    public IReadOnlyList<PedestrianView> PedestrianViews => _pedestrianViews;
    public GameObject BarrierView => _barrierView;
    public int ObstaclesCount => _obstacleViews.Count;

    public bool LoadedOk { get; private set; }

    private void Start()
    {
        if (_autoStartOnPlay && _levelId > 0)
            LaunchLevel(_levelId);
    }

    public bool LaunchLevel(int levelId)
    {
        if (!LevelLoader.TryLoad(levelId, out LevelData level, out string error))
        {
            Debug.LogError("GameLauncher could not start level " + levelId + ": " + error);
            LoadedOk = false;
            return false;
        }
        return LaunchLevel(level);
    }

    public bool LaunchLevel(LevelData level)
    {
        if (level == null)
        {
            LoadedOk = false;
            return false;
        }

        ResolveReferences();
        if (_gridController == null || _gameManager == null)
        {
            Debug.LogError("GameLauncher needs a GridController and GameManager in the scene");
            LoadedOk = false;
            return false;
        }

        ClearRig();

        _gridController.SetGridSize(level.gridWidth, level.gridHeight);
        _gameManager.InitializeLevel(level);
        SpawnRig(level);

        CurrentLevel = level;
        _levelId = level.id;
        LoadedOk = true;
        if (_sessionStats != null) _sessionStats.Reset();
        if (_cameraRig != null) _cameraRig.Frame(_gridController);
        return true;
    }

    private void ResolveReferences()
    {
        if (_gridController == null) _gridController = FindFirstObjectByType<GridController>();
        if (_gameManager == null) _gameManager = FindFirstObjectByType<GameManager>();
        if (_sessionStats == null) _sessionStats = FindFirstObjectByType<LevelSessionStats>();
        if (_cameraRig == null) _cameraRig = FindFirstObjectByType<GameCameraRig>();
    }

    private void ClearRig()
    {
        foreach (var vehicle in _vehicles)
        {
            if (vehicle != null) Destroy(vehicle.gameObject);
        }
        _vehicles.Clear();

        foreach (var view in _pedestrianViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _pedestrianViews.Clear();

        foreach (var view in _obstacleViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _obstacleViews.Clear();

        if (_barrierView != null) Destroy(_barrierView);
        _barrierView = null;
    }

    private void SpawnRig(LevelData level)
    {
        SpawnVehicles(level);
        SpawnPedestrians(level);
        SpawnObstacles(level);
        SpawnBarrier(level);
    }

    private void SpawnVehicles(LevelData level)
    {
        if (level.vehicles == null) return;
        foreach (var data in level.vehicles)
        {
            Vector3Int position = new Vector3Int(data.tiles[0].x, data.tiles[0].y, 0);
            Orientation orientation = data.orientation == "vertical"
                ? Orientation.Vertical
                : Orientation.Horizontal;

            GameObject go = InstantiateVehiclePrefab(data.tiles.Length);
            go.name = data.id;
            go.transform.position = _gridController.CellToWorld(position);

            var vehicle = go.GetComponent<Vehicle>();
            vehicle.Initialize(data.id, orientation, position, data.tiles.Length);

            var movement = go.GetComponent<VehicleMovement>();
            if (movement != null) movement.Initialize(_gridController);

            _vehicles.Add(vehicle);
            _gameManager.RegisterVehicleOnMap(vehicle);
        }
    }

    private GameObject InstantiateVehiclePrefab(int length)
    {
        GameObject prefab = length >= 3 ? _busPrefab : length == 2 ? _truckPrefab : _carPrefab;
        if (prefab != null)
        {
            var instance = Instantiate(prefab);
            return instance;
        }

        var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Vector3 scale = fallback.transform.localScale;
        scale.x *= Mathf.Max(1, length);
        fallback.transform.localScale = scale;
        fallback.AddComponent<Vehicle>();
        fallback.AddComponent<VehicleMovement>();
        return fallback;
    }

    private void SpawnPedestrians(LevelData level)
    {
        if (level.pedestrians == null) return;
        for (int i = 0; i < level.pedestrians.Length; i++)
        {
            var data = level.pedestrians[i];
            Vector3Int start = new Vector3Int(data.route[0].x, data.route[0].y, 0);

            GameObject go = _pedestrianPrefab != null
                ? Instantiate(_pedestrianPrefab)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Pedestrian " + i;
            go.transform.position = _gridController.CellToWorld(start);

            var view = go.GetComponent<PedestrianView>();
            if (view == null) view = go.AddComponent<PedestrianView>();
            view.Initialize(_gameManager.Pedestrians[i], _gridController);
            _pedestrianViews.Add(view);
        }
    }

    private void SpawnObstacles(LevelData level)
    {
        if (level.staticObstacles == null) return;
        foreach (var data in level.staticObstacles)
        {
            Vector3Int tile = new Vector3Int(data.tile.x, data.tile.y, 0);

            GameObject go = _obstaclePrefab != null ? Instantiate(_obstaclePrefab) : CreateFallbackObstacle();
            go.name = "Obstacle " + tile.x + "," + tile.y;
            go.transform.position = _gridController.CellToWorld(tile);
            _obstacleViews.Add(go);
        }
    }

    private static GameObject CreateFallbackObstacle()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(1f, 1f, 0.8f);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            var material = new Material(shader);
            material.color = new Color(0.42f, 0.42f, 0.45f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        return go;
    }

    private void SpawnBarrier(LevelData level)
    {
        if (level.barriers == null || level.barriers.Length == 0) return;

        var data = level.barriers[0];
        Vector3Int tile = new Vector3Int(data.tile.x, data.tile.y, 0);

        GameObject go = _barrierPrefab != null ? Instantiate(_barrierPrefab) : CreateFallbackBarrier();
        go.name = "Barrier";
        go.transform.position = _gridController.CellToWorld(tile);
        go.transform.rotation = Quaternion.Euler(0f, BarrierFacingYaw(level), 0f);
        _barrierView = go;
    }

    private float BarrierFacingYaw(LevelData level)
    {
        if (level.exitTiles == null || level.exitTiles.Length == 0) return 0f;
        var exit = level.exitTiles[0];
        if (exit.y == level.gridHeight - 1) return -90f;
        if (exit.y == 0) return 90f;
        if (exit.x == 0) return 180f;
        return 0f;
    }

    private static GameObject CreateFallbackBarrier()
    {
        var go = new GameObject("Barrier");
        var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossbar.name = "Crossbar";
        crossbar.transform.SetParent(go.transform, false);
        crossbar.transform.localScale = new Vector3(1.5f, 0.2f, 0.2f);
        go.AddComponent<BarrierGate>().CrossbarPivot = crossbar.transform;
        return go;
    }
}
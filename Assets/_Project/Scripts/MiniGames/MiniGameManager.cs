using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    private MiniGameController _activeController;
    private string _activeSceneName;
    private readonly List<MonoBehaviour> _pausedMainBehaviours = new List<MonoBehaviour>();
    private readonly List<EventSystem> _disabledMainEventSystems = new List<EventSystem>();

    public bool IsMiniGameActive => _activeController != null;
    public string ActiveSceneName => _activeController != null ? _activeSceneName : null;

    public event Action OnMiniGameCompleted;

    public static MiniGameManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var host = new GameObject("MiniGameManager");
        return host.AddComponent<MiniGameManager>();
    }

    public void LoadMiniGame(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || _activeController != null) return;
        if (!MiniGameCatalog.TryParseSceneName(sceneName, out var type, out var difficulty))
        {
            Debug.LogError("Unknown mini game scene: " + sceneName);
            return;
        }

        foreach (var inputHandler in FindObjectsByType<InputHandler>(FindObjectsInactive.Include))
        {
            inputHandler.enabled = false;
            _pausedMainBehaviours.Add(inputHandler);
        }

        foreach (var eventSystem in FindObjectsByType<EventSystem>(FindObjectsInactive.Include))
        {
            eventSystem.enabled = false;
            _disabledMainEventSystems.Add(eventSystem);
        }

        var rigHost = new GameObject(sceneName + "Rig");
        _activeSceneName = sceneName;
        switch (type)
        {
            case MiniGameType.Pipes:
            {
                var controller = rigHost.AddComponent<PipeMiniGameController>();
                controller.Spec = MiniGameCatalog.Pipes(difficulty);
                _activeController = controller;
                break;
            }
            case MiniGameType.Pattern:
            {
                var controller = rigHost.AddComponent<PatternMiniGameController>();
                controller.Spec = MiniGameCatalog.Pattern(difficulty);
                _activeController = controller;
                break;
            }
            default:
            {
                var controller = rigHost.AddComponent<MemoryMiniGameController>();
                controller.Spec = MiniGameCatalog.Memory(difficulty);
                _activeController = controller;
                break;
            }
        }
    }

    public void CompleteMiniGame()
    {
        if (_activeController == null) return;

        OnMiniGameCompleted?.Invoke();

        foreach (var eventSystem in _disabledMainEventSystems)
        {
            if (eventSystem != null) eventSystem.enabled = true;
        }
        _disabledMainEventSystems.Clear();

        foreach (var behaviour in _pausedMainBehaviours)
        {
            if (behaviour != null) behaviour.enabled = true;
        }
        _pausedMainBehaviours.Clear();

        if (_activeController != null)
            Destroy(_activeController.gameObject);
        _activeController = null;
        _activeSceneName = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
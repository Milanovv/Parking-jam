using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUiController : MonoBehaviour
{
    public static GameUiController Instance { get; private set; }

    [SerializeField] private GameLauncher _launcher;
    [SerializeField] private LevelSessionStats _session;
    [SerializeField] private LevelHud _hud;
    [SerializeField] private LevelSelectController _levelSelect;
    [SerializeField] private ShopController _shop;
    [SerializeField] private PauseController _pause;
    [SerializeField] private SettingsController _settings;
    [SerializeField] private Canvas _canvas;

    private GameObject _menuScreen;
    private GameObject _levelSelectScreen;
    private GameObject _shopScreen;
    private GameObject _settingsScreen;
    private GameObject _pauseScreen;
    private GameObject _hudScreen;
    private GameObject _overlay;
    private Transform _levelGridParent;
    private Transform _shopCardsParent;

    private readonly List<GameObject> _screens = new List<GameObject>();

    public bool IsShowingMenu => _menuScreen != null && _menuScreen.activeSelf;
    public bool IsShowingLevelSelect => _levelSelectScreen != null && _levelSelectScreen.activeSelf;
    public bool IsShowingShop => _shopScreen != null && _shopScreen.activeSelf;
    public bool IsShowingHud => _hudScreen != null && _hudScreen.activeSelf;
    public bool OverlayActive => _overlay != null && _overlay.activeSelf;
    public bool IsPauseVisible => _pause != null && _pause.IsPaused;
    public GameLauncher Launcher => _launcher;
    public PauseController Pause => _pause;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResolveReferences();
        BuildScreens();
    }

    private void Start()
    {
        if (_launcher != null) _launcher.AutoStartOnPlay = false;
        ShowMenu();
    }

    private void Update()
    {
        RefreshOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void ResolveReferences()
    {
        if (_launcher == null) _launcher = FindFirstObjectByType<GameLauncher>();
        if (_session == null) _session = FindFirstObjectByType<LevelSessionStats>();
        if (_hud == null) _hud = FindFirstObjectByType<LevelHud>();
        if (_levelSelect == null) _levelSelect = GetComponent<LevelSelectController>() ?? gameObject.AddComponent<LevelSelectController>();
        if (_shop == null) _shop = GetComponent<ShopController>() ?? gameObject.AddComponent<ShopController>();
        if (_pause == null) _pause = GetComponent<PauseController>() ?? gameObject.AddComponent<PauseController>();
        if (_settings == null) _settings = GetComponent<SettingsController>() ?? gameObject.AddComponent<SettingsController>();
        if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null)
        {
            var host = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            _canvas = host.GetComponent<Canvas>();
        }
    }

    private void BuildScreens()
    {
        _menuScreen = GameUiFactory.CreateScreen(_canvas.transform, "MainMenu");
        _levelSelectScreen = GameUiFactory.CreateScreen(_canvas.transform, "LevelSelect");
        _shopScreen = GameUiFactory.CreateScreen(_canvas.transform, "Shop");
        _settingsScreen = GameUiFactory.CreateScreen(_canvas.transform, "Settings");

        _overlay = GameUiFactory.CreateOverlay(_canvas);
        _pauseScreen = GameUiFactory.CreateScreen(_canvas.transform, "PausePanel");

        if (_hud != null)
            _hudScreen = _hud.gameObject;
        else
            _hudScreen = GameUiFactory.CreateHudScreen(_canvas, _hud);

        _screens.Add(_menuScreen);
        _screens.Add(_levelSelectScreen);
        _screens.Add(_shopScreen);
        _screens.Add(_settingsScreen);
        _screens.Add(_hudScreen);
        _screens.Add(_pauseScreen);

        _levelGridParent = GameUiFactory.CreateGridParent(_levelSelectScreen.transform);
        _shopCardsParent = GameUiFactory.CreateListParent(_shopScreen.transform);

        var save = SaveState();
        _levelSelect.LevelChosen += StartLevel;
        _levelSelect.Build(save, _levelGridParent);
        _shop.Build(save, _shopCardsParent, HandleShopAction);

        _pause.PausePanel = _pauseScreen;
        BuildButtons();
    }

    private static SaveData SaveState()
    {
        var economy = EconomyManager.Instance;
        return economy != null && economy.State != null ? economy.State : new SaveData();
    }

    private void BuildButtons()
    {
        GameUiFactory.CreateText(_menuScreen.transform, "Title", "PARKING JAM", 72, Color.white, TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0f, 560f);
        GameUiFactory.CreateButton(_menuScreen.transform, "PlayButton", "Play", ShowLevelSelect, new Vector2(520f, 120f), new Color(0.20f, 0.55f, 0.30f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 80f);
        GameUiFactory.CreateButton(_menuScreen.transform, "ShopButton", "Shop", ShowShop, new Vector2(520f, 120f), new Color(0.35f, 0.40f, 0.60f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -80f);
        GameUiFactory.CreateButton(_menuScreen.transform, "SettingsButton", "Settings", ShowSettings, new Vector2(520f, 120f), new Color(0.45f, 0.35f, 0.25f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -240f);

        GameUiFactory.CreateText(_levelSelectScreen.transform, "Title", "Select Level", 56, Color.white, TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0f, 740f);
        GameUiFactory.CreateButton(_levelSelectScreen.transform, "BackButton", "Back", ShowMenu, new Vector2(280f, 90f), new Color(0.40f, 0.30f, 0.30f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -760f);

        GameUiFactory.CreateText(_shopScreen.transform, "Title", "Skin Shop", 56, Color.white, TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0f, 760f);
        GameUiFactory.CreateButton(_shopScreen.transform, "BackButton", "Back", ShowMenu, new Vector2(280f, 90f), new Color(0.40f, 0.30f, 0.30f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -820f);

        GameUiFactory.CreateText(_settingsScreen.transform, "Title", "Settings", 56, Color.white, TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0f, 500f);
        GameUiFactory.CreateText(_settingsScreen.transform, "SfxLabel", "SFX Volume", 34, Color.white, TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0f, 260f);

        Text sfxValue = GameUiFactory.CreateText(_settingsScreen.transform, "SfxValue",
            Mathf.RoundToInt(SettingsController.SfxVolume * 100f) + "%", 34, Color.white, TextAnchor.MiddleCenter);
        sfxValue.rectTransform.anchoredPosition = new Vector2(0f, 200f);

        GameUiFactory.CreateButton(_settingsScreen.transform, "SfxDownButton", "-", () =>
            {
                SettingsController.SfxVolume -= 0.1f;
                sfxValue.text = Mathf.RoundToInt(SettingsController.SfxVolume * 100f) + "%";
            }, new Vector2(120f, 90f), new Color(0.35f, 0.30f, 0.40f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(-160f, 200f);
        GameUiFactory.CreateButton(_settingsScreen.transform, "SfxUpButton", "+", () =>
            {
                SettingsController.SfxVolume += 0.1f;
                sfxValue.text = Mathf.RoundToInt(SettingsController.SfxVolume * 100f) + "%";
            }, new Vector2(120f, 90f), new Color(0.35f, 0.30f, 0.40f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(160f, 200f);

        GameUiFactory.CreateButton(_settingsScreen.transform, "BackButton", "Back", ShowMenu, new Vector2(280f, 90f), new Color(0.40f, 0.30f, 0.30f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -400f);

        GameUiFactory.CreateText(_pauseScreen.transform, "Title", "Paused", 56, Color.white, TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0f, 300f);
        GameUiFactory.CreateButton(_pauseScreen.transform, "ResumeButton", "Resume", ResumeGame, new Vector2(460f, 100f), new Color(0.20f, 0.55f, 0.30f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);
        GameUiFactory.CreateButton(_pauseScreen.transform, "RestartButton", "Restart", RestartLevel, new Vector2(460f, 100f), new Color(0.40f, 0.45f, 0.55f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -90f);
        GameUiFactory.CreateButton(_pauseScreen.transform, "ExitButton", "Exit to Menu", ExitToMenu, new Vector2(460f, 100f), new Color(0.55f, 0.30f, 0.30f))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -240f);
    }

    private void HandleShopAction(string skinId)
    {
        var economy = EconomyManager.Instance;
        if (economy == null || !SkinCatalog.Contains(skinId)) return;

        if (economy.EquipSkin(skinId))
        {
            _shop.Refresh(SaveState(), HandleShopAction);
            return;
        }

        var entry = SkinCatalog.Find(skinId);
        bool bought = entry.Exclusive
            ? economy.TryBuyExclusiveSkin(skinId)
            : economy.TryBuyCommonSkin(skinId);
        if (bought) economy.EquipSkin(skinId);
        _shop.Refresh(SaveState(), HandleShopAction);
    }

    private void SetActiveScreen(GameObject screen)
    {
        foreach (var candidate in _screens)
        {
            if (candidate != null) candidate.SetActive(candidate == screen);
        }
    }

    public void ShowMenu()
    {
        if (_pause != null && _pause.IsPaused) _pause.Resume();
        SetActiveScreen(_menuScreen);
        RefreshOverlay();
    }

    public void ShowLevelSelect()
    {
        _levelSelect.Build(SaveState(), _levelGridParent);
        SetActiveScreen(_levelSelectScreen);
    }

    public void ShowShop()
    {
        _shop.Refresh(SaveState(), HandleShopAction);
        SetActiveScreen(_shopScreen);
    }

    public void ShowSettings()
    {
        SetActiveScreen(_settingsScreen);
    }

    public void StartLevel(int levelId)
    {
        if (_launcher == null || !_launcher.LaunchLevel(levelId)) return;
        if (_session != null) _session.Reset();
        SetActiveScreen(_hudScreen);
        RefreshOverlay();
    }

    public void ShowPause()
    {
        if (_pause == null) return;
        _pause.ShowPause();
        RefreshOverlay();
    }

    public void ResumeGame()
    {
        if (_pause != null) _pause.Resume();
        RefreshOverlay();
    }

    public void RestartLevel()
    {
        int levelId = _launcher != null ? _launcher.LevelId : 1;
        if (_pause != null && _pause.IsPaused) _pause.Resume();
        StartLevel(levelId);
    }

    public void ExitToMenu()
    {
        if (_pause != null && _pause.IsPaused) _pause.Resume();
        ShowMenu();
    }

    public void RefreshOverlay()
    {
        if (_overlay == null) return;
        bool miniGameActive = MiniGameManager.Instance != null && MiniGameManager.Instance.IsMiniGameActive;
        bool shouldShow = (_pause != null && _pause.IsPaused) || miniGameActive;
        if (_overlay.activeSelf != shouldShow) _overlay.SetActive(shouldShow);
    }
}

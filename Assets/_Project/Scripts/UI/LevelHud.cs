using UnityEngine;
using UnityEngine.UI;

public class LevelHud : MonoBehaviour
{
    [SerializeField] private Text _movesText;
    [SerializeField] private Text _timerText;
    [SerializeField] private Text _undosText;
    [SerializeField] private Text _coinsText;
    [SerializeField] private Text _keysText;
    [SerializeField] private Button _coinSkipButton;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Text _tutorialCueText;
    [SerializeField] private LevelSessionStats _stats;

    public Text MovesText
    {
        get => _movesText;
        set => _movesText = value;
    }

    public Text TimerText
    {
        get => _timerText;
        set => _timerText = value;
    }

    public Text UndosText
    {
        get => _undosText;
        set => _undosText = value;
    }

    public Text CoinsText
    {
        get => _coinsText;
        set => _coinsText = value;
    }

    public Text KeysText
    {
        get => _keysText;
        set => _keysText = value;
    }

    public Button CoinSkipButton
    {
        get => _coinSkipButton;
        set => _coinSkipButton = value;
    }

    public Button PauseButton
    {
        get => _pauseButton;
        set => _pauseButton = value;
    }

    public Text TutorialCue
    {
        get => _tutorialCueText;
        set => _tutorialCueText = value;
    }

    public LevelSessionStats Stats
    {
        get => _stats;
        set => _stats = value;
    }

    private void Awake()
    {
        if (_coinSkipButton != null)
        {
            _coinSkipButton.onClick.AddListener(() =>
            {
                var gameManager = GameManager.Instance;
                if (gameManager != null) gameManager.TryCoinSkip();
            });
        }

        if (_pauseButton != null)
        {
            _pauseButton.onClick.AddListener(() =>
            {
                var ui = GameUiController.Instance;
                if (ui != null) ui.ShowPause();
            });
        }
    }

    private void Update()
    {
        if (_stats == null) _stats = FindFirstObjectByType<LevelSessionStats>();
        Refresh();
    }

    public void Refresh()
    {
        var gameManager = GameManager.Instance;
        int moves = _stats != null ? _stats.MovesIssued : (gameManager != null ? gameManager.Tick : 0);
        float time = _stats != null ? _stats.ElapsedPlayTime : 0f;
        int undos = gameManager != null ? gameManager.UndoBalance : 0;

        var economy = EconomyManager.Instance;
        int coins = economy != null && economy.State != null ? economy.State.coins : 0;
        int keys = economy != null && economy.State != null ? economy.State.keys : 0;

        if (_movesText != null) _movesText.text = "Moves: " + moves;
        if (_timerText != null) _timerText.text = FormatTime(time);
        if (_undosText != null) _undosText.text = "Undos: " + undos;
        if (_coinsText != null) _coinsText.text = coins.ToString();
        if (_keysText != null) _keysText.text = keys.ToString();
        RefreshCoinSkipVisibility();
        RefreshTutorialCueVisibility();
    }

    private void RefreshCoinSkipVisibility()
    {
        if (_coinSkipButton == null) return;
        var gameManager = GameManager.Instance;
        bool visible = gameManager != null
            && gameManager.Gate != null
            && gameManager.Gate.Locked
            && gameManager.BarrierTile.HasValue;
        if (_coinSkipButton.gameObject.activeSelf != visible)
            _coinSkipButton.gameObject.SetActive(visible);

        var economy = EconomyManager.Instance;
        int coins = economy != null && economy.State != null ? economy.State.coins : 0;
        bool usable = gameManager != null
            && gameManager.CanRequestBarrierUnlock
            && coins >= EconomyConfig.CoinSkipPriceCoins;
        _coinSkipButton.interactable = usable;
    }

    private void RefreshTutorialCueVisibility()
    {
        if (_tutorialCueText == null) return;
        var gameManager = GameManager.Instance;
        bool visible = gameManager != null && gameManager.CanRequestBarrierUnlock;
        if (_tutorialCueText.gameObject.activeSelf != visible)
            _tutorialCueText.gameObject.SetActive(visible);
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return string.Format("{0}:{1:00}", total / 60, total % 60);
    }
}
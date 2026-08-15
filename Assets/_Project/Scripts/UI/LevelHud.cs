using UnityEngine;
using UnityEngine.UI;

public class LevelHud : MonoBehaviour
{
    [SerializeField] private Text _movesText;
    [SerializeField] private Text _timerText;
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

    public LevelSessionStats Stats
    {
        get => _stats;
        set => _stats = value;
    }

    private void Update()
    {
        if (_stats == null) _stats = FindFirstObjectByType<LevelSessionStats>();
        if (_stats == null) return;

        if (_movesText != null)
            _movesText.text = "Moves: " + _stats.MovesIssued;
        if (_timerText != null)
            _timerText.text = FormatTime(_stats.ElapsedPlayTime);
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return string.Format("{0}:{1:00}", total / 60, total % 60);
    }
}
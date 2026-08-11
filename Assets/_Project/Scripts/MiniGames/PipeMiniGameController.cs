using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PipeMiniGameController : MiniGameController
{
    public PipeSpec Spec;
    public bool IsSolved => _board != null && _board.IsConnected();
    public int RemainingSeconds => _timedOut ? 0 : Mathf.CeilToInt(_remainingSeconds);
    public PipeBoard Board => _board;

    private PipeBoard _board;
    private readonly List<Image> _tileViews = new List<Image>();
    private Image _background;
    private Text _timerText;
    private float _remainingSeconds;
    private bool _timedOut;

    private void Start()
    {
        BuildView();
        NewBoard();
    }

    private void Update()
    {
        if (_timerText == null || Spec.TimeLimitSeconds <= 0 || _timedOut || IsSolved) return;

        _remainingSeconds -= Time.deltaTime;
        _timerText.text = Mathf.CeilToInt(_remainingSeconds) + "s";
        if (_remainingSeconds <= 0f)
        {
            _timedOut = true;
            Retry();
        }
    }

    private void BuildView()
    {
        Canvas canvas = BuildCanvas();
        EnsureEventSystem();

        var background = new GameObject("Background", typeof(Image));
        background.transform.SetParent(canvas.transform, false);
        _background = background.GetComponent<Image>();
        _background.color = MiniGameGraphics.Background;
        _background.rectTransform.anchorMin = Vector2.zero;
        _background.rectTransform.anchorMax = Vector2.one;
        _background.rectTransform.sizeDelta = Vector2.zero;

        if (Spec.TimeLimitSeconds > 0)
        {
            var timer = new GameObject("Timer", typeof(Text));
            timer.transform.SetParent(canvas.transform, false);
            _timerText = timer.GetComponent<Text>();
            _timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _timerText.fontSize = 64;
            _timerText.color = Color.white;
            _timerText.alignment = TextAnchor.MiddleCenter;
            var timerRect = _timerText.rectTransform;
            timerRect.anchorMin = new Vector2(0f, 1f);
            timerRect.anchorMax = new Vector2(1f, 1f);
            timerRect.offsetMin = new Vector2(0f, -140f);
            timerRect.offsetMax = new Vector2(0f, -40f);
        }

        if (Spec.Hints > 0)
        {
            var hintButton = new GameObject("Hint", typeof(Button), typeof(Image));
            hintButton.transform.SetParent(canvas.transform, false);
            var hintImage = hintButton.GetComponent<Image>();
            hintImage.color = MiniGameGraphics.HintGlow;
            var hintText = new GameObject("Text", typeof(Text));
            hintText.transform.SetParent(hintButton.transform, false);
            var hintLabel = hintText.GetComponent<Text>();
            hintLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintLabel.text = "Hint x" + Spec.Hints;
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.rectTransform.anchorMin = Vector2.zero;
            hintLabel.rectTransform.anchorMax = Vector2.one;
            hintLabel.rectTransform.sizeDelta = Vector2.zero;
            hintButton.GetComponent<Button>().onClick.AddListener(UseHint);
        }
    }

    private void NewBoard()
    {
        _timedOut = false;
        _remainingSeconds = Spec.TimeLimitSeconds;
        _board = PipeBoard.Generate(Spec.Width, Spec.Height, Spec.RotatableTiles, new System.Random());
        RebuildTiles();
        if (_timerText != null) _timerText.text = _remainingSeconds + "s";
    }

    private void RebuildTiles()
    {
        foreach (var view in _tileViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _tileViews.Clear();

        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        float tileSize = 132f;
        float spacing = 12f;
        float totalWidth = _board.Width * tileSize + (_board.Width - 1) * spacing;
        float totalHeight = _board.Height * tileSize + (_board.Height - 1) * spacing;

        for (int x = 0; x < _board.Width; x++)
        {
            for (int y = 0; y < _board.Height; y++)
            {
                var tile = new GameObject("Tile " + x + "," + y, typeof(Image));
                tile.transform.SetParent(canvas.transform, false);
                var image = tile.GetComponent<Image>();
                _tileViews.Add(image);

                var rect = image.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(tileSize, tileSize);
                float offsetX = (x + 0.5f) * (tileSize + spacing) - totalWidth * 0.5f;
                float offsetY = (y + 0.5f) * (tileSize + spacing) - totalHeight * 0.5f;
                rect.anchoredPosition = new Vector2(offsetX, offsetY);

                RefreshTile(x, y);
            }
        }
    }

    private void RefreshTile(int x, int y)
    {
        var board = _board;
        int index = y * board.Width + x;
        if (index < 0 || index >= _tileViews.Count) return;

        var image = _tileViews[index];
        var tile = board.Tile(x, y);
        var open = PipeDirections.OpenMask(tile.Type, tile.Rotation);

        image.sprite = MiniGameGraphics.PipeSprite(open);
        if (tile.Type == PipeTileType.Source)
            image.color = MiniGameGraphics.SourceColor;
        else if (tile.Type == PipeTileType.Sink)
            image.color = MiniGameGraphics.SinkColor;
        else if (board.IsRotatable(x, y))
            image.color = PipeDirections.Contains(open, PipeDirections.Up) || PipeDirections.Contains(open, PipeDirections.Left)
                ? MiniGameGraphics.PipeColor
                : MiniGameGraphics.TileBase;
        else
            image.color = MiniGameGraphics.TileBase;
    }

    public bool RotateTileAt(int x, int y)
    {
        if (_board == null || IsSolved) return false;
        if (!_board.TryRotate(x, y)) return false;

        RefreshTile(x, y);
        if (_board.IsConnected()) CompleteWin();
        return true;
    }

    private void UseHint()
    {
        if (_board == null) return;
        var hint = _board.HintTile();
        if (!hint.HasValue) return;

        int index = hint.Value.y * _board.Width + hint.Value.x;
        if (index < 0 || index >= _tileViews.Count) return;
        _tileViews[index].color = MiniGameGraphics.HintGlow;
    }

    public override void Retry()
    {
        NewBoard();
    }
}
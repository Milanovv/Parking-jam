using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryMiniGameController : MiniGameController
{
    public MemorySpec Spec;
    public bool IsWon => _game != null && _game.IsWon;
    public bool IsAnimating => _animationCoroutine != null;

    private MemoryFlips _game;
    private readonly List<Image> _cardViews = new List<Image>();
    private Coroutine _animationCoroutine;
    private Text _moveText;
    private int _lastFirstCard = -1;

    private void Start()
    {
        BuildView();
        NewGame();
    }

    private void BuildView()
    {
        Canvas canvas = BuildCanvas();
        EnsureEventSystem();

        var background = new GameObject("Background", typeof(Image));
        background.transform.SetParent(canvas.transform, false);
        var bgImage = background.GetComponent<Image>();
        bgImage.color = MiniGameGraphics.Background;
        bgImage.rectTransform.anchorMin = Vector2.zero;
        bgImage.rectTransform.anchorMax = Vector2.one;
        bgImage.rectTransform.sizeDelta = Vector2.zero;

        if (Spec.MoveLimit > 0)
        {
            var moveLabel = new GameObject("Moves", typeof(Text));
            moveLabel.transform.SetParent(canvas.transform, false);
            _moveText = moveLabel.GetComponent<Text>();
            _moveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _moveText.fontSize = 56;
            _moveText.color = Color.white;
            _moveText.alignment = TextAnchor.MiddleCenter;
            var moveRect = _moveText.rectTransform;
            moveRect.anchorMin = new Vector2(0f, 1f);
            moveRect.anchorMax = new Vector2(1f, 1f);
            moveRect.offsetMin = new Vector2(0f, -160f);
            moveRect.offsetMax = new Vector2(0f, -80f);
        }
    }

    private void NewGame()
    {
        _game = new MemoryFlips(Spec.Pairs, Spec.Width, Spec.Height, Spec.MoveLimit > 0 ? Spec.MoveLimit : (int?)null, new System.Random());
        RebuildCards();
        if (_moveText != null) _moveText.text = "Moves " + _game.MovesUsed + "/" + Spec.MoveLimit;
    }

    private void RebuildCards()
    {
        foreach (var view in _cardViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _cardViews.Clear();

        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        float cardSize = 170f;
        float spacing = 16f;
        float totalWidth = _game.Width * cardSize + (_game.Width - 1) * spacing;
        float totalHeight = _game.Height * cardSize + (_game.Height - 1) * spacing;

        for (int i = 0; i < _game.Layout.Count; i++)
        {
            int x = i % _game.Width;
            int y = i / _game.Width;

            var card = new GameObject("Card " + i, typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            var image = card.GetComponent<Image>();
            image.sprite = MiniGameGraphics.CardBackSprite();
            image.color = MiniGameGraphics.BackColor;

            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(cardSize, cardSize);
            float offsetX = (x + 0.5f) * (cardSize + spacing) - totalWidth * 0.5f;
            float offsetY = (y + 0.5f) * (cardSize + spacing) - totalHeight * 0.5f;
            rect.anchoredPosition = new Vector2(offsetX, offsetY);

            int index = i;
            var button = card.AddComponent<Button>();
            button.onClick.AddListener(() => FlipCard(index));

            _cardViews.Add(image);
        }
    }

    public MemoryFlipResult FlipCard(int index)
    {
        if (_game == null || _animationCoroutine != null) return MemoryFlipResult.IgnoredLocked;

        var result = _game.Flip(index);
        if (result == MemoryFlipResult.Matched)
        {
            SetFront(_lastFirstCard);
            SetFront(index);
            if (_moveText != null) _moveText.text = "Moves " + _game.MovesUsed + "/" + Spec.MoveLimit;
            if (_game.IsWon) CompleteWin();
        }
        else if (result == MemoryFlipResult.Mismatched)
        {
            SetFront(_lastFirstCard);
            SetFront(index);
            if (_moveText != null) _moveText.text = "Moves " + _game.MovesUsed + "/" + Spec.MoveLimit;
            PlayFlipBack(_lastFirstCard, index);
        }
        else if (result == MemoryFlipResult.Lost)
        {
            SetFront(_lastFirstCard);
            SetFront(index);
            _animationCoroutine = StartCoroutine(RetryAfterDelay());
        }
        else if (result == MemoryFlipResult.Revealed)
        {
            _lastFirstCard = index;
            SetFront(index);
        }
        return result;
    }

    private void SetFront(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;
        _cardViews[cardIndex].sprite = FrontSprite(cardIndex);
        _cardViews[cardIndex].color = Color.white;
    }

    private void PlayFlipBack(int firstCard, int secondCard)
    {
        _animationCoroutine = StartCoroutine(FlipBackAfterDelay(firstCard, secondCard));
    }

    private IEnumerator FlipBackAfterDelay(int firstCard, int secondCard)
    {
        yield return new WaitForSeconds(0.8f);
        _game.ResolveMismatch();
        SetBack(firstCard);
        SetBack(secondCard);
        _animationCoroutine = null;
    }

    private void SetBack(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;
        _cardViews[cardIndex].sprite = MiniGameGraphics.CardBackSprite();
        _cardViews[cardIndex].color = MiniGameGraphics.BackColor;
    }

    private Sprite FrontSprite(int index)
    {
        return MiniGameGraphics.ShapeSprite(_game.PairOf(index) % 6, MiniGameGraphics.ButtonColors[_game.PairOf(index) % 6]);
    }

    private IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        _animationCoroutine = null;
        Retry();
    }

    public override void Retry()
    {
        NewGame();
    }
}
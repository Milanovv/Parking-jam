using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PatternMiniGameController : MiniGameController
{
    public PatternSpec Spec;
    public bool IsPlayingSequence => _sequenceCoroutine != null;
    public bool IsComplete => _round != null && _round.IsComplete;

    private PatternLockRound _round;
    private readonly List<Button> _buttons = new List<Button>();
    private readonly List<AudioSource> _tones = new List<AudioSource>();
    private Coroutine _sequenceCoroutine;
    private Text _statusText;

    private static readonly float[] Frequencies =
    {
        660f, 440f, 990f, 770f, 550f, 880f
    };

    private void Start()
    {
        BuildView();
        RoundStart();
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

        var status = new GameObject("Status", typeof(Text));
        status.transform.SetParent(canvas.transform, false);
        _statusText = status.GetComponent<Text>();
        _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _statusText.fontSize = 56;
        _statusText.color = Color.white;
        _statusText.alignment = TextAnchor.MiddleCenter;
        var statusRect = _statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(0f, -200f);
        statusRect.offsetMax = new Vector2(0f, -100f);

        var positions = LayoutFor(Spec.ButtonCount);
        for (int i = 0; i < Spec.ButtonCount; i++)
        {
            var button = new GameObject("Button " + i, typeof(Button), typeof(Image));
            button.transform.SetParent(canvas.transform, false);
            var image = button.GetComponent<Image>();
            image.sprite = MiniGameGraphics.ShapeSprite(i, MiniGameGraphics.ButtonColors[i]);
            image.color = Color.white;

            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(180f, 180f);
            rect.anchoredPosition = new Vector2(positions[i].x, positions[i].y);

            var audio = button.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.clip = MiniGameGraphics.SineClip(Frequencies[i]);

            var uiButton = button.GetComponent<Button>();
            int index = i;
            uiButton.onClick.AddListener(() => TapButton(index));

            _buttons.Add(uiButton);
            _tones.Add(audio);
        }
    }

    private static Vector2[] LayoutFor(int buttonCount)
    {
        switch (buttonCount)
        {
            case 4:
                return new[]
                {
                    new Vector2(-200f, 200f), new Vector2(200f, 200f),
                    new Vector2(-200f, -200f), new Vector2(200f, -200f)
                };
            case 5:
                return new[]
                {
                    new Vector2(0f, 300f), new Vector2(-260f, 0f), new Vector2(260f, 0f),
                    new Vector2(-130f, -300f), new Vector2(130f, -300f)
                };
            default:
                return new[]
                {
                    new Vector2(-220f, 260f), new Vector2(0f, 260f), new Vector2(220f, 260f),
                    new Vector2(-220f, -260f), new Vector2(0f, -260f), new Vector2(220f, -260f)
                };
        }
    }

    private void RoundStart()
    {
        _round = new PatternLockRound(Spec.ButtonCount, Spec.SequenceLength);
        _round.Generate(new System.Random());
        if (_statusText != null) _statusText.text = "";
        PlaySequence();
    }

    private void PlaySequence()
    {
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(Playback());
    }

    private IEnumerator Playback()
    {
        SetButtonsEnabled(false);
        yield return new WaitForSeconds(0.3f);

        foreach (int buttonIndex in _round.Sequence)
        {
            var button = _buttons[buttonIndex];
            button.image.color = Color.white;
            _tones[buttonIndex].Play();
            yield return new WaitForSeconds(Spec.FlashSeconds);
            button.image.color = MiniGameGraphics.ButtonColors[buttonIndex];
            yield return new WaitForSeconds(0.15f);
        }

        _buttons.ForEach(b => b.image.color = MiniGameGraphics.ButtonColors[_buttons.IndexOf(b)]);
        yield return new WaitForSeconds(0.3f);
        SetButtonsEnabled(true);
        _sequenceCoroutine = null;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        foreach (var button in _buttons)
            button.interactable = enabled;
    }

    public PatternTapResult TapButton(int index)
    {
        if (_round == null || IsPlayingSequence) return PatternTapResult.Wrong;

        var result = _round.Submit(index);
        _tones[index].Play();

        if (result == PatternTapResult.Complete)
        {
            if (_statusText != null) _statusText.text = "Unlocked";
            CompleteWin();
        }
        else if (result == PatternTapResult.Wrong)
        {
            if (_statusText != null) _statusText.text = "Wrong";
            Retry();
        }
        return result;
    }

    public void SkipPlayback()
    {
        if (_sequenceCoroutine == null) return;
        StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = null;
        SetButtonsEnabled(true);
    }

    public int[] CopySequence()
    {
        return _round != null ? new List<int>(_round.Sequence).ToArray() : System.Array.Empty<int>();
    }

    public override void Retry()
    {
        RoundStart();
    }
}
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class GameUiFactory
{
    private static Font _font;

    public static GameObject CreateScreen(Transform parent, string name, bool withBackground = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        go.SetActive(false);
        if (withBackground)
            CreateImage(go.transform, "Background", new Color(0f, 0f, 0f, 0.85f));
        return go;
    }

    public static GameObject CreateOverlay(Canvas canvas)
    {
        var go = new GameObject("GameOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        Stretch(go.GetComponent<RectTransform>());
        var image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5f);
        image.raycastTarget = true;
        go.SetActive(false);
        return go;
    }

    public static GameObject CreateHudScreen(Canvas canvas, LevelHud hud)
    {
        var go = new GameObject("HUD", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    public static Transform CreateGridParent(Transform screen)
    {
        var go = new GameObject("LevelGrid", typeof(RectTransform));
        go.transform.SetParent(screen, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 1350f);
        rect.anchoredPosition = new Vector2(0f, -60f);

        var layout = go.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(150f, 150f);
        layout.spacing = new Vector2(25f, 25f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        layout.childAlignment = TextAnchor.MiddleCenter;
        return go.transform;
    }

    public static Transform CreateListParent(Transform screen)
    {
        var go = new GameObject("Cards", typeof(RectTransform));
        go.transform.SetParent(screen, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 1500f);
        rect.anchoredPosition = new Vector2(0f, -100f);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        return go.transform;
    }

    public static Button CreateLevelButton(Transform parent, int levelId, bool unlocked, UnityAction onClick)
    {
        var go = new GameObject("Level " + levelId, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        button.interactable = unlocked;
        button.onClick.AddListener(onClick);

        go.GetComponent<Image>().color = unlocked
            ? new Color(0.20f, 0.45f, 0.75f)
            : new Color(0.28f, 0.28f, 0.30f);

        Text label = CreateText(go.transform, "Label", levelId.ToString(), 40, Color.white, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform);
        return button;
    }

    public static GameObject CreateShopCard(Transform parent, SkinCatalog.Entry entry, bool owned, bool equipped, UnityAction action)
    {
        var go = new GameObject(entry.Id + " Card", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(960f, 130f);
        go.GetComponent<Image>().color = equipped
            ? new Color(0.14f, 0.48f, 0.26f)
            : new Color(0.22f, 0.22f, 0.24f);

        CreateText(go.transform, "Name", entry.DisplayName, 38, Color.white, TextAnchor.MiddleLeft)
            .rectTransform.anchoredPosition = new Vector2(48f, 30f);

        string stateLabel = equipped ? "Equipped" : owned ? "Owned" : entry.Exclusive ? "Exclusive" : "Common";
        CreateText(go.transform, "State", stateLabel, 26, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleLeft)
            .rectTransform.anchoredPosition = new Vector2(48f, -22f);

        string actionLabel = owned
            ? (equipped ? "Equipped" : "Equip")
            : (entry.Exclusive ? "1 Key" : EconomyConfig.CommonSkinPriceCoins + " Coins");
        bool actionable = !equipped;

        Button button = CreateButton(go.transform, "Action", actionLabel, action, new Vector2(240f, 92f),
            actionable ? new Color(0.25f, 0.50f, 0.80f) : new Color(0.35f, 0.35f, 0.38f));
        button.interactable = actionable;
        var buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-48f, 0f);
        return go;
    }

    public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = color;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = CreateText(go.transform, "Text", label, 30, Color.white, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.font = Font();
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    public static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Font Font()
    {
        if (_font == null)
        {
            foreach (var name in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
            {
                try
                {
                    _font = Resources.GetBuiltinResource<Font>(name);
                    break;
                }
                catch
                {
                    // try the next built-in font name
                }
            }
        }
        return _font;
    }
}

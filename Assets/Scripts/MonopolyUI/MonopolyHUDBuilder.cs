using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MonopolyHUDBuilder
{
    public static MonopolyHUDReferences Build(string rootName = "Monopoly HUD", MonopolyHUDLayoutSettings layoutSettings = null)
    {
        MonopolyHUDLayoutSettings settings = layoutSettings != null
            ? layoutSettings
            : MonopolyHUDLayoutSettings.CreateDefaultInstance();

        EnsureEventSystem();

        GameObject root = new GameObject(rootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = settings.sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = settings.referenceResolution;
        scaler.matchWidthOrHeight = settings.matchWidthOrHeight;

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();

        MonopolyHUDReferences references = new MonopolyHUDReferences
        {
            canvas = canvas,
            canvasScaler = scaler,
            raycaster = raycaster
        };

        references.statsPanel = CreatePanel(root.transform, "Stats Panel", settings.statsPanel, settings.panelColor, settings);
        references.moneyText = CreateText(references.statsPanel, "Money Text", settings.moneyText, settings);
        references.incomeText = CreateText(references.statsPanel, "Income Text", settings.incomeText, settings);

        references.infoPanel = CreatePanel(root.transform, "Info Panel", settings.infoPanel, settings.panelColor, settings);
        references.infoText = CreateText(references.infoPanel, "Info Text", settings.infoText, settings);

        references.upgradeButton = CreateButton(root.transform, "Upgrade Button", settings.upgradeButton, settings.upgradeButtonText, settings, out Text upgradeText);
        references.upgradeButtonText = upgradeText;

        references.diceButton = CreateButton(root.transform, "Dice Button", settings.diceButton, settings.diceButtonText, settings, out Text diceText);
        references.diceButtonText = diceText;

        return references;
    }

    private static RectTransform CreatePanel(Transform parent, string name, MonopolyUILayoutRect layout, Color backgroundColor, MonopolyHUDLayoutSettings settings)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        ApplyLayout(rect, layout);

        Image image = panel.GetComponent<Image>();
        image.color = backgroundColor;

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = settings.outlineColor;
        outline.effectDistance = settings.outlineDistance;

        return rect;
    }

    private static Button CreateButton(Transform parent, string name, MonopolyUILayoutRect layout, MonopolyUITextLayout textLayout, MonopolyHUDLayoutSettings settings, out Text labelText)
    {
        RectTransform rect = CreatePanel(parent, name, layout, settings.buttonColor, settings);
        GameObject buttonObject = rect.gameObject;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.normalColor = settings.buttonColor;
        colors.highlightedColor = settings.buttonHighlightColor;
        colors.pressedColor = settings.buttonPressedColor;
        colors.disabledColor = settings.buttonDisabledColor;
        button.colors = colors;

        labelText = CreateText(rect, name + " Text", textLayout, settings);
        return button;
    }

    private static Text CreateText(RectTransform parent, string name, MonopolyUITextLayout textLayout, MonopolyHUDLayoutSettings settings)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(textLayout.padding.left, textLayout.padding.bottom);
        rect.offsetMax = new Vector2(-textLayout.padding.right, -textLayout.padding.top);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = textLayout.defaultText;
        text.fontSize = textLayout.fontSize;
        text.color = settings.textColor;
        text.alignment = textLayout.alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;
        return text;
    }

    private static void ApplyLayout(RectTransform rect, MonopolyUILayoutRect layout)
    {
        rect.anchorMin = layout.anchorMin;
        rect.anchorMax = layout.anchorMax;
        rect.pivot = layout.pivot;
        rect.anchoredPosition = layout.anchoredPosition;
        rect.sizeDelta = layout.sizeDelta;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

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
        references.moneyIcon = CreateImage(references.statsPanel, "Money Icon", settings.moneyIcon, settings.moneyIconSprite, settings);
        references.moneyText = CreateText(references.statsPanel, "Money Text", settings.moneyText, settings);
        references.turnText = CreateText(references.statsPanel, "Turn Text", settings.turnText, settings);
        references.incomeText = CreateText(references.statsPanel, "Income Text", settings.incomeText, settings);
        references.victoryInfoText = CreateText(references.statsPanel, "Victory Info Text", settings.victoryInfoText, settings);

        references.infoPanel = CreatePanel(root.transform, "Info Panel", settings.infoPanel, settings.panelColor, settings);
        references.infoText = CreateText(references.infoPanel, "Info Text", settings.infoText, settings);

        references.buildButton = CreateButton(root.transform, "Build Button", settings.buildButton, settings.buildButtonText, settings, out Text buildText);
        references.buildButtonText = buildText;

        references.upgradeButton = CreateButton(root.transform, "Upgrade Button", settings.upgradeButton, settings.upgradeButtonText, settings, out Text upgradeText, true);
        references.upgradeButtonText = upgradeText;
        references.upgradeButtonIcon = CreateImage(references.upgradeButton.GetComponent<RectTransform>(), "Upgrade Icon", settings.upgradeButtonIcon, settings.upgradeButtonSprite, settings);

        references.diceButton = CreateButton(root.transform, "Dice Button", settings.diceButton, settings.diceButtonText, settings, out Text diceText, true);
        references.diceButtonText = diceText;
        references.diceButtonIcon = CreateImage(references.diceButton.GetComponent<RectTransform>(), "Dice Icon", settings.diceButtonIcon, settings.advanceButtonSprite, settings);

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

    private static Button CreateButton(Transform parent, string name, MonopolyUILayoutRect layout, MonopolyUITextLayout textLayout, MonopolyHUDLayoutSettings settings, out Text labelText, bool transparentBackground = false)
    {
        RectTransform rect = transparentBackground
            ? CreatePanel(parent, name, layout, Color.clear, settings)
            : CreatePanel(parent, name, layout, settings.buttonColor, settings);
        GameObject buttonObject = rect.gameObject;
        Button button = buttonObject.AddComponent<Button>();
        Image buttonImage = buttonObject.GetComponent<Image>();
        button.targetGraphic = buttonImage;

        if (transparentBackground)
        {
            Outline outline = buttonObject.GetComponent<Outline>();
            if (outline != null)
            {
                Object.Destroy(outline);
            }
        }

        ColorBlock colors = button.colors;
        colors.normalColor = transparentBackground ? Color.clear : settings.buttonColor;
        colors.highlightedColor = transparentBackground ? new Color(1f, 1f, 1f, 0.08f) : settings.buttonHighlightColor;
        colors.pressedColor = transparentBackground ? new Color(1f, 1f, 1f, 0.14f) : settings.buttonPressedColor;
        colors.disabledColor = transparentBackground ? Color.clear : settings.buttonDisabledColor;
        button.colors = colors;

        labelText = CreateText(rect, name + " Text", textLayout, settings);
        return button;
    }

    private static Image CreateImage(RectTransform parent, string name, MonopolyUIImageLayout imageLayout, Sprite sprite, MonopolyHUDLayoutSettings settings)
    {
        if (imageLayout == null)
        {
            imageLayout = new MonopolyUIImageLayout();
        }

        if (imageLayout.padding == null)
        {
            imageLayout.padding = new MonopolyUIPadding();
        }

        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(imageLayout.padding.left, imageLayout.padding.bottom);
        rect.offsetMax = new Vector2(-imageLayout.padding.right, -imageLayout.padding.top);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite == null ? Color.clear : Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(RectTransform parent, string name, MonopolyUITextLayout textLayout, MonopolyHUDLayoutSettings settings)
    {
        if (textLayout == null)
        {
            textLayout = new MonopolyUITextLayout();
        }

        if (textLayout.padding == null)
        {
            textLayout.padding = new MonopolyUIPadding();
        }

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

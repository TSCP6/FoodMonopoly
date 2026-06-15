using System;
using UnityEngine;

[Serializable]
public class MonopolyUIPadding
{
    public int left;
    public int right;
    public int top;
    public int bottom;

    public MonopolyUIPadding()
    {
        left = 18;
        right = 18;
        top = 18;
        bottom = 18;
    }

    public MonopolyUIPadding(int left, int right, int top, int bottom)
    {
        this.left = left;
        this.right = right;
        this.top = top;
        this.bottom = bottom;
    }
}

[Serializable]
public class MonopolyUILayoutRect
{
    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public Vector2 pivot;
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;

    public MonopolyUILayoutRect()
    {
    }

    public MonopolyUILayoutRect(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        this.anchorMin = anchorMin;
        this.anchorMax = anchorMax;
        this.pivot = pivot;
        this.anchoredPosition = anchoredPosition;
        this.sizeDelta = sizeDelta;
    }
}

[Serializable]
public class MonopolyUITextLayout
{
    public string defaultText;
    public int fontSize;
    public TextAnchor alignment;
    public MonopolyUIPadding padding = new MonopolyUIPadding();

    public MonopolyUITextLayout()
    {
    }

    public MonopolyUITextLayout(string defaultText, int fontSize, TextAnchor alignment, MonopolyUIPadding padding)
    {
        this.defaultText = defaultText;
        this.fontSize = fontSize;
        this.alignment = alignment;
        this.padding = padding;
    }
}

[CreateAssetMenu(menuName = "Food Monopoly/UI/HUD Layout Settings", fileName = "MonopolyHUDLayoutSettings")]
public class MonopolyHUDLayoutSettings : ScriptableObject
{
    [Header("Canvas")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;
    public int sortingOrder = 100;

    [Header("Colors")]
    public Color panelColor = new Color(1f, 1f, 1f, 0.62f);
    public Color buttonColor = new Color(1f, 1f, 1f, 0.68f);
    public Color buttonHighlightColor = new Color(0.92f, 0.97f, 1f, 0.82f);
    public Color buttonPressedColor = new Color(0.84f, 0.92f, 1f, 0.9f);
    public Color buttonDisabledColor = new Color(0.8f, 0.8f, 0.8f, 0.45f);
    public Color textColor = Color.black;
    public Color outlineColor = Color.black;
    public Vector2 outlineDistance = new Vector2(4f, -4f);

    [Header("Panels")]
    public MonopolyUILayoutRect statsPanel = new MonopolyUILayoutRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(470f, 118f));
    public MonopolyUILayoutRect infoPanel = new MonopolyUILayoutRect(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 48f), new Vector2(770f, 290f));
    public MonopolyUILayoutRect buildButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-555f, 55f), new Vector2(185f, 165f));
    public MonopolyUILayoutRect upgradeButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-350f, 55f), new Vector2(185f, 165f));
    public MonopolyUILayoutRect diceButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 55f), new Vector2(285f, 265f));

    [Header("Text")]
    public MonopolyUITextLayout moneyText = new MonopolyUITextLayout("金钱：0", 32, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 18, 58));
    public MonopolyUITextLayout incomeText = new MonopolyUITextLayout("每回合收入：0", 32, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 58, 18));
    public MonopolyUITextLayout infoText = new MonopolyUITextLayout("信息栏", 31, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 32, 28, 28));
    public MonopolyUITextLayout buildButtonText = new MonopolyUITextLayout("建造选项\n默认建造\n连锁餐厅", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
    public MonopolyUITextLayout upgradeButtonText = new MonopolyUITextLayout("升级选项\n点击后进入\n升级模式", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
    public MonopolyUITextLayout diceButtonText = new MonopolyUITextLayout("骰子选项\n点击后进入下一\n回合", 34, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));

    public static MonopolyHUDLayoutSettings CreateDefaultInstance()
    {
        MonopolyHUDLayoutSettings settings = CreateInstance<MonopolyHUDLayoutSettings>();
        settings.ResetToDefaults();
        return settings;
    }

    [ContextMenu("Reset To Defaults")]
    public void ResetToDefaults()
    {
        referenceResolution = new Vector2(1920f, 1080f);
        matchWidthOrHeight = 0.5f;
        sortingOrder = 100;

        panelColor = new Color(1f, 1f, 1f, 0.62f);
        buttonColor = new Color(1f, 1f, 1f, 0.68f);
        buttonHighlightColor = new Color(0.92f, 0.97f, 1f, 0.82f);
        buttonPressedColor = new Color(0.84f, 0.92f, 1f, 0.9f);
        buttonDisabledColor = new Color(0.8f, 0.8f, 0.8f, 0.45f);
        textColor = Color.black;
        outlineColor = Color.black;
        outlineDistance = new Vector2(4f, -4f);

        statsPanel = new MonopolyUILayoutRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(470f, 118f));
        infoPanel = new MonopolyUILayoutRect(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 48f), new Vector2(770f, 290f));
        buildButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-555f, 55f), new Vector2(185f, 165f));
        upgradeButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-350f, 55f), new Vector2(185f, 165f));
        diceButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 55f), new Vector2(285f, 265f));

        moneyText = new MonopolyUITextLayout("金钱：0", 32, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 18, 58));
        incomeText = new MonopolyUITextLayout("每回合收入：0", 32, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 58, 18));
        infoText = new MonopolyUITextLayout("信息栏", 31, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 32, 28, 28));
        buildButtonText = new MonopolyUITextLayout("建造选项\n默认建造\n连锁餐厅", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
        upgradeButtonText = new MonopolyUITextLayout("升级选项\n点击后进入\n升级模式", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
        diceButtonText = new MonopolyUITextLayout("骰子选项\n点击后进入下一\n回合", 34, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
    }
}

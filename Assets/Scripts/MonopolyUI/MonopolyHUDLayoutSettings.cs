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

[Serializable]
public class MonopolyUIImageLayout
{
    public MonopolyUIPadding padding = new MonopolyUIPadding();

    public MonopolyUIImageLayout()
    {
    }

    public MonopolyUIImageLayout(MonopolyUIPadding padding)
    {
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

    [Header("Sprites")]
    public Sprite advanceButtonSprite;
    public Sprite upgradeButtonSprite;
    public Sprite moneyIconSprite;

    [Header("Panels")]
    public MonopolyUILayoutRect statsPanel = new MonopolyUILayoutRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(610f, 240f));
    public MonopolyUILayoutRect infoPanel = new MonopolyUILayoutRect(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 48f), new Vector2(770f, 380f));
    public MonopolyUILayoutRect buildButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-555f, 55f), new Vector2(185f, 165f));
    public MonopolyUILayoutRect upgradeButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-350f, 55f), new Vector2(185f, 165f));
    public MonopolyUILayoutRect diceButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 55f), new Vector2(285f, 265f));

    [Header("Text")]
    public MonopolyUITextLayout moneyText = new MonopolyUITextLayout("\u91D1\u94B1\uFF1A0", 30, TextAnchor.UpperLeft, new MonopolyUIPadding(92, 24, 16, 190));
    public MonopolyUITextLayout turnText = new MonopolyUITextLayout("\u56DE\u5408\uFF1A0/0", 30, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 54, 152));
    public MonopolyUITextLayout incomeText = new MonopolyUITextLayout("\u6BCF\u56DE\u5408\u6536\u5165\uFF1A0", 30, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 92, 114));
    public MonopolyUITextLayout victoryInfoText = new MonopolyUITextLayout("\u83B7\u80DC\u4FE1\u606F\uFF1A\u9650\u5B9A\u56DE\u5408\u7ED3\u675F\u524D\u91D1\u5E01\u6570\u8D85\u8FC7\u654C\u4EBA\n\u654C\u4EBA\u91D1\u5E01\u6570\uFF1A0", 24, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 132, 16));
    public MonopolyUITextLayout infoText = new MonopolyUITextLayout("信息栏", 24, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 32, 28, 28));
    public MonopolyUITextLayout buildButtonText = new MonopolyUITextLayout("建造选项\n默认建造\n连锁餐厅", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
    public MonopolyUITextLayout upgradeButtonText = new MonopolyUITextLayout("升级选项\n点击后进入\n升级模式", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
    public MonopolyUITextLayout diceButtonText = new MonopolyUITextLayout("骰子选项\n点击后进入下一\n回合", 34, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
    public MonopolyUIImageLayout moneyIcon = new MonopolyUIImageLayout(new MonopolyUIPadding(28, 530, 18, 178));
    public MonopolyUIImageLayout upgradeButtonIcon = new MonopolyUIImageLayout(new MonopolyUIPadding(18, 18, 18, 18));
    public MonopolyUIImageLayout diceButtonIcon = new MonopolyUIImageLayout(new MonopolyUIPadding(18, 18, 18, 18));

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

        advanceButtonSprite = null;
        upgradeButtonSprite = null;
        moneyIconSprite = null;

        statsPanel = new MonopolyUILayoutRect(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(610f, 240f));
        infoPanel = new MonopolyUILayoutRect(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 48f), new Vector2(770f, 380f));
        buildButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-555f, 55f), new Vector2(185f, 165f));
        upgradeButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-350f, 55f), new Vector2(185f, 165f));
        diceButton = new MonopolyUILayoutRect(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 55f), new Vector2(285f, 265f));

        moneyText = new MonopolyUITextLayout("\u91D1\u94B1\uFF1A0", 30, TextAnchor.UpperLeft, new MonopolyUIPadding(92, 24, 16, 190));
        turnText = new MonopolyUITextLayout("\u56DE\u5408\uFF1A0/0", 30, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 54, 152));
        incomeText = new MonopolyUITextLayout("\u6BCF\u56DE\u5408\u6536\u5165\uFF1A0", 30, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 92, 114));
        victoryInfoText = new MonopolyUITextLayout("\u83B7\u80DC\u4FE1\u606F\uFF1A\u9650\u5B9A\u56DE\u5408\u7ED3\u675F\u524D\u91D1\u5E01\u6570\u8D85\u8FC7\u654C\u4EBA\n\u654C\u4EBA\u91D1\u5E01\u6570\uFF1A0", 24, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 24, 132, 16));
        infoText = new MonopolyUITextLayout("信息栏", 24, TextAnchor.UpperLeft, new MonopolyUIPadding(32, 32, 28, 28));
        buildButtonText = new MonopolyUITextLayout("建造选项\n默认建造\n连锁餐厅", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
        upgradeButtonText = new MonopolyUITextLayout("升级选项\n点击后进入\n升级模式", 28, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
        diceButtonText = new MonopolyUITextLayout("骰子选项\n点击后进入下一\n回合", 34, TextAnchor.MiddleCenter, new MonopolyUIPadding(18, 18, 18, 18));
    }
}

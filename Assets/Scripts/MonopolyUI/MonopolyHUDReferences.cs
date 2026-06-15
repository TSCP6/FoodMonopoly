using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MonopolyHUDReferences
{
    [Header("Canvas")]
    public Canvas canvas;
    public CanvasScaler canvasScaler;
    public GraphicRaycaster raycaster;

    [Header("Stats Panel")]
    public RectTransform statsPanel;
    public Text moneyText;
    public Text incomeText;

    [Header("Info Panel")]
    public RectTransform infoPanel;
    public Text infoText;

    [Header("Action Buttons")]
    public Button buildButton;
    public Text buildButtonText;
    public Button upgradeButton;
    public Text upgradeButtonText;
    public Button diceButton;
    public Text diceButtonText;

    public bool HasRequiredReferences()
    {
        return moneyText != null
            && incomeText != null
            && infoText != null
            && buildButton != null
            && upgradeButton != null
            && diceButton != null;
    }
}

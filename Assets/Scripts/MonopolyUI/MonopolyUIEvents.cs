using System;
using UnityEngine.Events;

public enum MonopolyUIActionType
{
    Build,
    RollDiceOrNextTurn,
    EnterUpgradeMode,
    ConfirmUpgrade
}

[Serializable]
public class MonopolyUIActionEvent : UnityEvent<MonopolyUIActionType>
{
}

[Serializable]
public class BoardGridViewEvent : UnityEvent<BoardGridView>
{
}

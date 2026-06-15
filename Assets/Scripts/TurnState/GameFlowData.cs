using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OptionalActionContext
{
    public PlayerData currentPlayer;
    public BoardGridView currentGridView;
    public GridData currentGrid;
    public List<GridData> upgradableGrids = new List<GridData>();
    public bool canBuild;
    public bool canUpgrade;
}

[Serializable]
public class StateMachineSettings
{
    [Range(1, 100)] public int turnsPerLevel = 30;
    public int levelCount = 2;
    public int initialMoney = 50;
    public bool carryMoneyBetweenLevels = false;
}

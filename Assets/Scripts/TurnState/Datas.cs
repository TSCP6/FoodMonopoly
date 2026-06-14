using System;
using System.Collections.Generic;

[Serializable]
public class BuildingData //建造信息
{
    public BuildingType buildingType = BuildingType.ChainRestaurant;
    public int level = 1; //建筑的等级

    public bool IsMaxLevel //判断是不是最大等级
    {
        get { return level >= 3; }
    }
}

[Serializable]
public class GridData //格子数据
{
    public int index; //格子索引
    public GridKind kind = GridKind.Building; //格子类型
    public int ownerPlayerId = -1; //拥有该格子的玩家ID（建筑格子）
    public BuildingData buildingData; //如果是建筑格子，存储建造信息

    public bool HasBuilding //判断格子上是否有建筑
    {
        get { return buildingData != null; }
    }
}

[Serializable]
public class PlayerData //玩家数据
{
    public int playerId; //玩家ID
    public string playerName; //玩家名称
    public PlayerKind playerKind = PlayerKind.Player; //玩家类型（玩家或敌人）
    public int money = 1000; //初始金钱
    public int position; //玩家在地图上的位置（格子索引）
    public List<int> ownedGridIndexes = new List<int>(); //拥有的建筑格子索引列表

    public bool IsBankrupt //判断玩家是否破产
    {
        get { return money <= 0; }
    }
}
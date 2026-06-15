// 定义了格子相关的规则和计算方法
public static class GridRules
{
    // 建造第 1 级建筑的费用。
    // 这里对应你表格里的“建设（建造一级建筑）价格”。
    public static int GetBuildCost(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.CrownRestaurant:
                return 20;
            case BuildingType.FineRestaurant:
                return 18;
            default: //默认是连锁餐厅
                return 12;
        }
    }

    // 升级费用根据“当前建筑类型 + 当前等级”计算。
    // currentLevel = 1 表示 1 级升 2 级，currentLevel = 2 表示 2 级升 3 级。
    public static int GetUpgradeCost(BuildingType type, int currentLevel)
    {
        switch (type)
        {
            case BuildingType.ChainRestaurant:
                return currentLevel == 1 ? 8 : currentLevel == 2 ? 8 : 0;
            case BuildingType.CrownRestaurant:
                return currentLevel == 1 ? 15 : currentLevel == 2 ? 15 : 0;
            case BuildingType.FineRestaurant:
                return currentLevel == 1 ? 18 : currentLevel == 2 ? 12 : 0;
            default:
                return 0;
        }
    }

    // 每回合收益根据“建筑类型 + 当前等级”计算。
    // 1级/2级/3级分别对应不同的回合收益。
    public static int GetTurnIncome(BuildingData buildingData)
    {
        if (buildingData == null)
        {
            return 0;
        }

        switch (buildingData.buildingType)
        {
            case BuildingType.ChainRestaurant:
                return buildingData.level == 1 ? 2 : buildingData.level == 2 ? 3 : 4;
            case BuildingType.CrownRestaurant:
                return buildingData.level == 1 ? 3 : buildingData.level == 2 ? 4 : 5;
            case BuildingType.FineRestaurant:
                return buildingData.level == 1 ? 3 : buildingData.level == 2 ? 6 : 9;
            default:
                return 0;
        }
    }

    // 过路费同样按“建筑类型 + 当前等级”计算。
    public static int GetPassFee(BuildingData buildingData)
    {
        if (buildingData == null)
        {
            return 0;
        }

        switch (buildingData.buildingType)
        {
            case BuildingType.ChainRestaurant:
                return buildingData.level == 1 ? 6 : buildingData.level == 2 ? 8 : 10;
            case BuildingType.CrownRestaurant:
                return buildingData.level == 1 ? 12 : buildingData.level == 2 ? 15 : 20;
            case BuildingType.FineRestaurant:
                return buildingData.level == 1 ? 4 : buildingData.level == 2 ? 6 : 8;
            default:
                return 0;
        }
    }
}
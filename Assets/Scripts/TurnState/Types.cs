public enum GridKind //格子类型
{
    Building, //建筑格子
    Event //事件格子
}

public enum PlayerKind //玩家类型
{
    Player, //玩家
    Enemy //敌人
}

public enum BuildingType //建筑类型
{
    ChainRestaurant, //连锁餐厅
    CrownRestaurant, //皇冠餐厅
    FineRestaurant //精致餐厅
}

public enum OptionalActionType //选择类型
{
    Skip, //跳过
    Build, //建造
    Upgrade //升级
}
//回合状态枚举

public enum TurnState
{
    Idle, //默认状态
    TurnStart, //回合开始
    RollDice, //掷骰子
    Move, //玩家移动
    ResolveGrid, //执行对应格子的事件或建造
    OptionalAction, //如果是建造进入建造选项
    EndTurn, //回合结束
    NextPlayer, //敌方回合
    CheckLevelEnd, //检查关卡是否结束
    GameOver //两关都结束时游戏结束
}
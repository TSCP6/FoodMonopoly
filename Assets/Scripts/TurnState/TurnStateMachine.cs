using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 回合状态机只负责流程控制，不负责地图生成或 UI。
// 地图已经由场景里的 36 个 cube 搭好，所以这里直接读取 BoardGridRegistry 的格子。
public class TurnStateMachine : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private BoardGridRegistry boardGridRegistry; //地图登记信息

    [Header("Game Settings")]
    [SerializeField] private StateMachineSettings settings = new StateMachineSettings();
    [SerializeField] private List<PlayerData> players = new List<PlayerData>();
    [SerializeField] private float moveStepDelay = 0.12f; //移动时间间隔

    public TurnState CurrentState { get; private set; } = TurnState.Idle; //状态
    public int CurrentLevelIndex { get; private set; } //关卡索引
    public int CurrentTurnInLevel { get; private set; } //当前关卡的回合数
    public int CurrentPlayerIndex { get; private set; } //当前玩家索引
    public int LastDiceValue { get; private set; } //上次掷的骰子点数

    public event Action<TurnState> OnStateChanged; //状态改变事件
    public event Action<int> OnLevelChanged; //关卡改变事件
    public event Action<PlayerData> OnPlayerChanged; //玩家改变事件
    public event Action<PlayerData, int> OnMoneyChanged; //金钱改变事件
    public event Action<PlayerData, GridData> OnGridResolved; //格子结算事件
    public event Action<OptionalActionContext> OnOptionalActionRequested; //可选行动请求事件
    public event Action<string> OnMessage; //消息事件

    private bool isInitialized; //是否已初始化
    private bool isBusy; //是否正在执行流程（如移动），防止重复触发

    private void Awake()
    {
        if (boardGridRegistry == null)
        {
            boardGridRegistry = FindObjectOfType<BoardGridRegistry>(); //自动寻找地图登记组件
        }
    }

    private void Start()
    {
        InitializeGame();
        EnterState(TurnState.TurnStart);
    }

    // 初始化玩家和场景棋盘。
    // 不再生成地图，只读取场景里已经摆好的 cube。
    private void InitializeGame()
    {
        if (isInitialized)
        {
            return;
        }

        CreatePlayersIfNeeded();
        RefreshBoardFromScene();

        CurrentLevelIndex = 0;
        CurrentTurnInLevel = 0;
        CurrentPlayerIndex = 0;
        isInitialized = true;

        LogMessage("Turn state machine initialized.");
    }

    // 如果场景里没放玩家数据，就自动创建最小可运行的玩家列表。
    private void CreatePlayersIfNeeded()
    {
        if (players == null)
        {
            players = new List<PlayerData>();
        }

        if (players.Count > 0)
        {
            //如果已经有玩家数据了，就只初始化 ID、金钱和位置，名字如果没设置就默认。
            for (int i = 0; i < players.Count; i++)
            {
                PlayerData player = players[i];
                player.playerId = i;
                player.money = settings.initialMoney;
                player.position = 0;
                if (string.IsNullOrEmpty(player.playerName))
                {
                    player.playerName = i == 0 ? "Player" : "Enemy" + i;
                }
            }

            return;
        }

        //默认创建两个玩家：Player 和 Enemy1，初始金钱由设置决定。
        for (int i = 0; i < 2; i++)
        {
            PlayerData player = new PlayerData();
            player.playerId = i;
            player.playerName = i == 0 ? "Player" : "Enemy" + i;
            player.playerKind = i == 0 ? PlayerKind.Player : PlayerKind.Enemy;
            player.money = settings.initialMoney;
            player.position = 0;
            players.Add(player);
        }
    }

    // 从场景里重新读取所有格子。
    // BoardGridRegistry 会按 GridIndex 排序，所以这里默认你的 36 个 cube 都已经挂好了 BoardGridView。
    private void RefreshBoardFromScene()
    {
        if (boardGridRegistry == null)
        {
            LogMessage("BoardGridRegistry missing.");
            return;
        }

        boardGridRegistry.Refresh();

        for (int i = 0; i < boardGridRegistry.Count; i++)
        {
            BoardGridView view = boardGridRegistry.GetView(i);
            if (view != null)
            {
                view.SyncFromScene();
            }
        }
    }

    // 状态切换统一入口。
    // 所有回合流程都从这里跳转，避免在别处直接乱改 CurrentState。
    private void EnterState(TurnState nextState)
    {
        CurrentState = nextState;
        OnStateChanged?.Invoke(CurrentState);

        switch (CurrentState)
        {
            case TurnState.TurnStart:
                HandleTurnStart();
                break;
            case TurnState.RollDice:
                HandleRollDice();
                break;
            case TurnState.Move:
                HandleMove();
                break;
            case TurnState.ResolveGrid:
                HandleResolveGrid();
                break;
            case TurnState.OptionalAction:
                HandleOptionalAction();
                break;
            case TurnState.EndTurn:
                HandleEndTurn();
                break;
            case TurnState.NextPlayer:
                HandleNextPlayer();
                break;
            case TurnState.CheckLevelEnd:
                HandleCheckLevelEnd();
                break;
            case TurnState.GameOver:
                LogMessage("Game over.");
                break;
        }
    }

    private void HandleTurnStart() //每回合开始时检查玩家状态，跳过破产玩家。
    {
        if (isBusy)
        {
            return;
        }

        PlayerData player = GetCurrentPlayer();
        if (player == null || player.IsBankrupt)
        {
            EnterState(TurnState.NextPlayer);
            return;
        }

        LogMessage("Turn start: " + player.playerName);
        OnPlayerChanged?.Invoke(player);
        EnterState(TurnState.RollDice);
    }

    private void HandleRollDice() //掷骰子，记录点数并进入移动状态。
    {
        LastDiceValue = UnityEngine.Random.Range(1, 7);
        LogMessage(GetCurrentPlayer().playerName + " rolled " + LastDiceValue);
        EnterState(TurnState.Move);
    }

    private void HandleMove() //根据骰子点数移动玩家位置。
    {
        if (!isBusy)
        {
            StartCoroutine(MoveRoutine());
        }
    }

    private IEnumerator MoveRoutine()
    {
        isBusy = true;

        PlayerData player = GetCurrentPlayer();
        int boardSize = GetBoardSize();

        for (int i = 0; i < LastDiceValue; i++) //每步移动后等待一段时间，模拟动画效果。
        {
            player.position = (player.position + 1) % boardSize;
            LogMessage(player.playerName + " moved to grid " + player.position);

            yield return new WaitForSeconds(moveStepDelay);
        }

        isBusy = false;
        EnterState(TurnState.ResolveGrid);
    }

    private void HandleResolveGrid() //根据玩家停留的格子类型和状态执行结算逻辑。
    {
        BoardGridView view = GetCurrentGridView();
        PlayerData player = GetCurrentPlayer();

        if (view == null)
        {
            EnterState(TurnState.EndTurn);
            return;
        }

        GridData grid = view.RuntimeData;

        if (view.IsBuildingGrid)
        {
            LogMessage(player.playerName + " landed on building grid " + grid.index);

            //如果格子上有建筑，并且不是玩家自己的，就执行过路费结算。
            if (grid.HasBuilding && grid.ownerPlayerId >= 0 && grid.ownerPlayerId != player.playerId)
            {
                ResolvePassFee(player, grid);
            }
        }
        else if (view.IsEventGrid)
        {
            LogMessage(player.playerName + " landed on event grid " + grid.index + " tag: " + view.gameObject.tag);
            ResolveEventGrid(view, player);
        }

        OnGridResolved?.Invoke(player, grid);
        EnterState(TurnState.OptionalAction);
    }

    private void HandleOptionalAction() //玩家在这里可以选择建造、升级或跳过。敌人默认自动决策：先建造，再升级，最后跳过。
    {
        OptionalActionContext context = BuildOptionalActionContext();
        OnOptionalActionRequested?.Invoke(context);

        OptionalActionType action = DecideOptionalAction(context);
        if (action == OptionalActionType.Build)
        {
            TryBuildOnCurrentGrid(context.currentPlayer, context.currentGridView);
        }
        else if (action == OptionalActionType.Upgrade)
        {
            TryUpgradeGlobalBuilding(context.currentPlayer);
        }
        else
        {
            LogMessage(context.currentPlayer.playerName + " skipped optional action.");
        }

        EnterState(TurnState.EndTurn);
    }

    private void HandleEndTurn() //回合结束时结算收益，增加回合数，并检查是否需要切换玩家或进入下一关。
    {
        PlayerData player = GetCurrentPlayer();
        SettleTurnIncome(player);
        CurrentTurnInLevel++;
        EnterState(TurnState.CheckLevelEnd);
    }

    private void HandleNextPlayer() //切换到下一个玩家，如果所有玩家都已轮过一轮则进入下一回合。
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % players.Count;
        OnPlayerChanged?.Invoke(GetCurrentPlayer());
        EnterState(TurnState.TurnStart);
    }

    private void HandleCheckLevelEnd() //检查当前关卡是否完成，如果完成则进入下一关，否则继续下一玩家回合。
    {
        if (IsLevelComplete())
        {
            CurrentLevelIndex++;
            OnLevelChanged?.Invoke(CurrentLevelIndex);

            if (CurrentLevelIndex >= settings.levelCount)
            {
                EnterState(TurnState.GameOver);
                return;
            }

            ResetForNextLevel();
        }

        EnterState(TurnState.NextPlayer);
    }

    private bool IsLevelComplete()
    {
        return CurrentTurnInLevel >= settings.turnsPerLevel;
    }

    // 下一关时只重置回合状态和所有格子的归属，不重新生成地图。
    private void ResetForNextLevel()
    {
        CurrentTurnInLevel = 0;
        CurrentPlayerIndex = 0;

        for (int i = 0; i < players.Count; i++)
        {
            players[i].position = 0;
            players[i].ownedGridIndexes.Clear();

            if (!settings.carryMoneyBetweenLevels) //如果不允许金钱跨关卡，则重置金钱。
            {
                players[i].money = settings.initialMoney;
            }
        }

        ResetBoardState();
        LogMessage("Level " + CurrentLevelIndex + " started.");
    }

    private void ResetBoardState()
    {
        if (boardGridRegistry == null)
        {
            return;
        }

        for (int i = 0; i < boardGridRegistry.Count; i++)
        {
            BoardGridView view = boardGridRegistry.GetView(i);
            if (view != null)
            {
                view.ResetNeutralState(); //重置为无人状态，保留格子类型（建筑/事件）不变。
            }
        }
    }

    private PlayerData GetCurrentPlayer() //获取当前玩家数据
    {
        if (players == null || players.Count == 0)
        {
            return null;
        }

        return players[CurrentPlayerIndex % players.Count];
    }

    private BoardGridView GetCurrentGridView() //获取当前格子数据
    {
        PlayerData player = GetCurrentPlayer();
        if (player == null || boardGridRegistry == null)
        {
            return null;
        }

        return boardGridRegistry.GetView(player.position);
    }

    private int GetBoardSize() //获取格子数量
    {
        if (boardGridRegistry != null && boardGridRegistry.Count > 0)
        {
            return boardGridRegistry.Count;
        }

        return 36;
    }

    // 构建可选行动的上下文信息，供决策函数和 UI 使用。
    private OptionalActionContext BuildOptionalActionContext()
    {
        PlayerData player = GetCurrentPlayer();
        BoardGridView currentView = GetCurrentGridView();

        OptionalActionContext context = new OptionalActionContext();
        context.currentPlayer = player;
        context.currentGridView = currentView;
        context.currentGrid = currentView == null ? null : currentView.RuntimeData;
        context.upgradableGrids = GetUpgradableGrids(player);
        context.canBuild = CanBuildOnCurrentGrid(currentView);
        context.canUpgrade = context.upgradableGrids.Count > 0;
        return context;
    }

    // 敌人默认自动决策：先建造，再升级，最后跳过。
    // 玩家后续可以接 UI，把这里替换成按钮输入结果。
    private OptionalActionType DecideOptionalAction(OptionalActionContext context)
    {
        if (context == null || context.currentPlayer == null || context.currentGridView == null)
        {
            return OptionalActionType.Skip;
        }

        if (context.currentPlayer.playerKind == PlayerKind.Enemy)
        {
            if (context.canBuild && CanAffordBuild(context.currentPlayer, context.currentGridView))
            {
                return OptionalActionType.Build;
            }

            if (context.canUpgrade && CanAffordAnyUpgrade(context.currentPlayer, context.upgradableGrids))
            {
                return OptionalActionType.Upgrade;
            }

            return OptionalActionType.Skip;
        }

        if (context.canBuild && CanAffordBuild(context.currentPlayer, context.currentGridView))
        {
            return OptionalActionType.Build;
        }

        if (context.canUpgrade && CanAffordAnyUpgrade(context.currentPlayer, context.upgradableGrids))
        {
            return OptionalActionType.Upgrade;
        }

        return OptionalActionType.Skip;
    }

    // 只有建筑格才允许建造，而且该格子必须是空的。
    private bool CanBuildOnCurrentGrid(BoardGridView gridView)
    {
        if (gridView == null)
        {
            return false;
        }

        return gridView.IsBuildingGrid && !gridView.RuntimeData.HasBuilding;
    }

    // 建造费用固定，后续如果要支持多种建筑类型，这里改成根据建筑类型返回不同的费用即可。
    private bool CanAffordBuild(PlayerData player, BoardGridView gridView)
    {
        if (player == null || gridView == null || !CanBuildOnCurrentGrid(gridView))
        {
            return false;
        }

        return player.money >= GridRules.GetBuildCost(BuildingType.ChainRestaurant);
    }

    // 升级费用根据玩家名下所有可升级建筑中等级最低的那一格来判断。
    private bool CanAffordAnyUpgrade(PlayerData player, List<GridData> grids)
    {
        if (player == null || grids == null)
        {
            return false;
        }

        for (int i = 0; i < grids.Count; i++)
        {
            GridData grid = grids[i];
            if (grid == null || !grid.HasBuilding || grid.ownerPlayerId != player.playerId)
            {
                continue;
            }

            int upgradeCost = GridRules.GetUpgradeCost(grid.buildingData.buildingType, grid.buildingData.level);
            if (upgradeCost > 0 && player.money >= upgradeCost)
            {
                return true;
            }
        }

        return false;
    }

    // 建造固定在当前格子上执行。
    // 这里默认先建第一种建筑，后续如果要弹 UI 选择建筑类型，只需要把参数改掉。
    private void TryBuildOnCurrentGrid(PlayerData player, BoardGridView gridView)
    {
        if (player == null || gridView == null || !CanBuildOnCurrentGrid(gridView))
        {
            return;
        }

        int buildCost = GridRules.GetBuildCost(BuildingType.ChainRestaurant);
        if (player.money < buildCost)
        {
            LogMessage(player.playerName + " has not enough money to build.");
            return;
        }

        ChangeMoney(player, -buildCost);
        gridView.SetBuildingOwner(player, BuildingType.ChainRestaurant);

        if (!player.ownedGridIndexes.Contains(gridView.GridIndex))
        {
            player.ownedGridIndexes.Add(gridView.GridIndex);
        }

        LogMessage(player.playerName + " built a ChainRestaurant at grid " + gridView.GridIndex);
    }

    // 升级逻辑是全局选择：从己方全部建筑中找出等级最低、且未满级的那一格进行升级。
    private void TryUpgradeGlobalBuilding(PlayerData player)
    {
        BoardGridView target = FindLowestLevelUpgradableGrid(player);
        if (target == null)
        {
            LogMessage("No upgrade target found.");
            return;
        }

        int upgradeCost = GridRules.GetUpgradeCost(target.RuntimeData.buildingData.buildingType, target.RuntimeData.buildingData.level);
        if (player.money < upgradeCost)
        {
            LogMessage(player.playerName + " has not enough money to upgrade.");
            return;
        }

        ChangeMoney(player, -upgradeCost);
        target.UpgradeBuilding();
        LogMessage(player.playerName + " upgraded grid " + target.GridIndex + " to level " + target.RuntimeData.buildingData.level);
    }

    // 从玩家名下所有可升级的建筑中找出等级最低的那一格，作为升级目标。
    private BoardGridView FindLowestLevelUpgradableGrid(PlayerData player)
    {
        if (player == null || boardGridRegistry == null)
        {
            return null;
        }

        BoardGridView bestGrid = null;
        for (int i = 0; i < boardGridRegistry.Count; i++)
        {
            BoardGridView view = boardGridRegistry.GetView(i);
            if (view == null)
            {
                continue;
            }

            GridData grid = view.RuntimeData;
            if (!grid.HasBuilding || grid.ownerPlayerId != player.playerId || grid.buildingData.IsMaxLevel)
            {
                continue;
            }

            if (bestGrid == null || grid.buildingData.level < bestGrid.RuntimeData.buildingData.level)
            {
                bestGrid = view;
            }
        }

        return bestGrid;
    }

    // 获取玩家名下所有可升级的建筑格子列表，供 UI 显示和决策使用。
    private List<GridData> GetUpgradableGrids(PlayerData player)
    {
        List<GridData> result = new List<GridData>();
        if (player == null || boardGridRegistry == null)
        {
            return result;
        }

        for (int i = 0; i < boardGridRegistry.Count; i++)
        {
            BoardGridView view = boardGridRegistry.GetView(i);
            if (view == null)
            {
                continue;
            }

            GridData grid = view.RuntimeData;
            if (grid.HasBuilding && grid.ownerPlayerId == player.playerId && !grid.buildingData.IsMaxLevel)
            {
                result.Add(grid);
            }
        }

        return result;
    }

    // 事件格不在这里写具体内容，只保留统一入口。
    // 你已经用 Tag 区分 Attack / Buff / Debuff，后面直接在这里 switch 就行。
    private void ResolveEventGrid(BoardGridView view, PlayerData player)
    {
        switch (view.gameObject.tag)
        {
            case "Attack":
                LogMessage(player.playerName + " triggered Attack event on grid " + view.GridIndex);
                break;
            case "Buff":
                LogMessage(player.playerName + " triggered Buff event on grid " + view.GridIndex);
                break;
            case "Debuff":
                LogMessage(player.playerName + " triggered Debuff event on grid " + view.GridIndex);
                break;
            default:
                LogMessage(player.playerName + " triggered an unknown event on grid " + view.GridIndex);
                break;
        }
    }

    // 回合结束时结算当前玩家名下所有建筑的回合收益。
    private void SettleTurnIncome(PlayerData player)
    {
        if (player == null || boardGridRegistry == null)
        {
            return;
        }

        int income = 0;
        for (int i = 0; i < boardGridRegistry.Count; i++)
        {
            BoardGridView view = boardGridRegistry.GetView(i);
            if (view == null || !view.RuntimeData.HasBuilding)
            {
                continue;
            }

            if (view.RuntimeData.ownerPlayerId == player.playerId)
            {
                income += GridRules.GetTurnIncome(view.RuntimeData.buildingData);
            }
        }

        if (income > 0)
        {
            ChangeMoney(player, income);
            LogMessage(player.playerName + " gained turn income: " + income);
        }
    }

    // 处理过路费：当前玩家支付给建筑所有者。
    private void ResolvePassFee(PlayerData currentPlayer, GridData grid)
    {
        PlayerData owner = GetPlayerById(grid.ownerPlayerId);
        if (owner == null || grid.buildingData == null)
        {
            return;
        }

        int fee = GridRules.GetPassFee(grid.buildingData);
        int paidFee = Mathf.Min(currentPlayer.money, fee);
        ChangeMoney(currentPlayer, -paidFee);
        ChangeMoney(owner, paidFee);

        LogMessage(currentPlayer.playerName + " paid pass fee " + paidFee + " to " + owner.playerName);
    }

    private PlayerData GetPlayerById(int playerId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].playerId == playerId)
            {
                return players[i];
            }
        }

        return null;
    }

    private void ChangeMoney(PlayerData player, int delta)
    {
        if (player == null)
        {
            return;
        }

        player.money += delta;
        OnMoneyChanged?.Invoke(player, player.money);
    }

    // 这里只做一个最轻量的消息出口，调试时会在 Console 里看到流程。
    private void LogMessage(string message)
    {
        Debug.Log("[TurnStateMachine] " + message);
        OnMessage?.Invoke(message);
    }
}
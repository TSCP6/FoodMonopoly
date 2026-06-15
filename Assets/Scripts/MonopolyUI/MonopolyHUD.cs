using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MonopolyHUD : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private TurnStateMachine stateMachine;
    [SerializeField] private BoardGridRegistry boardRegistry;
    [SerializeField] private MonopolyHUDReferences ui = new MonopolyHUDReferences();

    [Header("Runtime Setup")]
    [SerializeField] private bool autoFindSceneObjects = true;
    [SerializeField] private bool createRuntimeUI = true;
    [SerializeField] private MonopolyHUDLayoutSettings layoutSettings;

    [Header("Fallback Values")]
    [SerializeField] private int fallbackMoney;
    [SerializeField] private int fallbackIncome;

    [Header("Events")]
    public MonopolyUIActionEvent OnActionClicked = new MonopolyUIActionEvent();
    public BoardGridViewEvent OnUpgradeConfirmed = new BoardGridViewEvent();

    private readonly List<string> recentMessages = new List<string>();
    private PlayerData currentPlayer;
    private OptionalActionContext lastOptionalContext;
    private BoardGridView selectedUpgradeGrid;
    private bool upgradeMode;
    private bool subscribed;

    public bool IsUpgradeMode => upgradeMode;

    private void Awake()
    {
        ResolveSceneReferences();

        if (createRuntimeUI && !ui.HasRequiredReferences())
        {
            ui = MonopolyHUDBuilder.Build("Monopoly HUD", layoutSettings);
        }

        WireButtons();
        RefreshAll();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetMoney(int money)
    {
        fallbackMoney = money;
        RefreshStats();
    }

    public void SetIncome(int income)
    {
        fallbackIncome = income;
        RefreshStats();
    }

    public void SetInfo(string message)
    {
        if (ui.infoText != null)
        {
            ui.infoText.text = string.IsNullOrEmpty(message) ? "信息栏" : message;
        }
    }

    public void EnterUpgradeMode()
    {
        upgradeMode = true;
        selectedUpgradeGrid = null;
        OnActionClicked.Invoke(MonopolyUIActionType.EnterUpgradeMode);
        SetInfo("升级模式\n点击自己的建筑查看升级价格和收益变化。\n再次点击同一建筑确认升级。");
    }

    public void LeaveUpgradeMode()
    {
        upgradeMode = false;
        selectedUpgradeGrid = null;
    }

    public void HandleBoardGridClicked(BoardGridView gridView)
    {
        if (gridView == null)
        {
            return;
        }

        if (!upgradeMode)
        {
            selectedUpgradeGrid = null;
            SetInfo(BuildGridInfo(gridView, false));
            return;
        }

        if (selectedUpgradeGrid == gridView && CanUpgradeGrid(gridView, out string blockedReason))
        {
            OnActionClicked.Invoke(MonopolyUIActionType.ConfirmUpgrade);
            OnUpgradeConfirmed.Invoke(gridView);
            SetInfo("已确认升级请求\n格子：" + gridView.GridIndex + "\n请在 OnUpgradeConfirmed 中接入真正的升级逻辑。");
            LeaveUpgradeMode();
            return;
        }

        selectedUpgradeGrid = gridView;
        string info = BuildGridInfo(gridView, true);
        if (!CanUpgradeGrid(gridView, out blockedReason))
        {
            info += "\n" + blockedReason;
        }
        else
        {
            info += "\n再次点击这个建筑确认升级。";
        }

        SetInfo(info);
    }

    private void ResolveSceneReferences()
    {
        if (!autoFindSceneObjects)
        {
            return;
        }

        if (stateMachine == null)
        {
            stateMachine = FindObjectOfType<TurnStateMachine>();
        }

        if (boardRegistry == null)
        {
            boardRegistry = FindObjectOfType<BoardGridRegistry>();
        }
    }

    private void WireButtons()
    {
        if (ui.upgradeButton != null)
        {
            ui.upgradeButton.onClick.RemoveListener(EnterUpgradeMode);
            ui.upgradeButton.onClick.AddListener(EnterUpgradeMode);
        }

        if (ui.diceButton != null)
        {
            ui.diceButton.onClick.RemoveListener(HandleDiceButtonClicked);
            ui.diceButton.onClick.AddListener(HandleDiceButtonClicked);
        }
    }

    private void Subscribe()
    {
        if (subscribed || stateMachine == null)
        {
            return;
        }

        stateMachine.OnPlayerChanged += HandlePlayerChanged;
        stateMachine.OnMoneyChanged += HandleMoneyChanged;
        stateMachine.OnGridResolved += HandleGridResolved;
        stateMachine.OnOptionalActionRequested += HandleOptionalActionRequested;
        stateMachine.OnStateChanged += HandleStateChanged;
        stateMachine.OnMessage += HandleMessage;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || stateMachine == null)
        {
            return;
        }

        stateMachine.OnPlayerChanged -= HandlePlayerChanged;
        stateMachine.OnMoneyChanged -= HandleMoneyChanged;
        stateMachine.OnGridResolved -= HandleGridResolved;
        stateMachine.OnOptionalActionRequested -= HandleOptionalActionRequested;
        stateMachine.OnStateChanged -= HandleStateChanged;
        stateMachine.OnMessage -= HandleMessage;
        subscribed = false;
    }

    private void HandlePlayerChanged(PlayerData player)
    {
        currentPlayer = player;
        RefreshStats();
    }

    private void HandleMoneyChanged(PlayerData player, int money)
    {
        if (currentPlayer == null || player == currentPlayer || player.playerId == currentPlayer.playerId)
        {
            currentPlayer = player;
            RefreshStats();
        }
    }

    private void HandleGridResolved(PlayerData player, GridData grid)
    {
        if (grid == null)
        {
            return;
        }

        AddMessage(player.playerName + " 到达格子 " + grid.index);
    }

    private void HandleOptionalActionRequested(OptionalActionContext context)
    {
        lastOptionalContext = context;
        currentPlayer = context == null ? currentPlayer : context.currentPlayer;
        RefreshStats();

        if (context == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("信息栏");
        builder.AppendLine("当前玩家：" + context.currentPlayer.playerName);

        if (context.currentGrid != null)
        {
            builder.AppendLine("当前格子：" + context.currentGrid.index);
        }

        if (context.canBuild)
        {
            int buildCost = GridRules.GetBuildCost(BuildingType.ChainRestaurant);
            builder.AppendLine("可建造：价格 " + buildCost + "，每回合收入 " + GridRules.GetTurnIncome(new BuildingData { buildingType = BuildingType.ChainRestaurant, level = 1 }));
        }

        if (context.canUpgrade)
        {
            builder.AppendLine("可升级建筑：" + context.upgradableGrids.Count + " 个");
        }

        if (!context.canBuild && !context.canUpgrade)
        {
            builder.AppendLine("当前没有可选建筑操作。");
        }

        SetInfo(builder.ToString().TrimEnd());
    }

    private void HandleStateChanged(TurnState state)
    {
        if (state == TurnState.GameOver)
        {
            SetButtonInteractable(ui.upgradeButton, false);
            SetButtonInteractable(ui.diceButton, false);
            SetInfo("游戏结束");
        }
        else
        {
            SetButtonInteractable(ui.upgradeButton, true);
            SetButtonInteractable(ui.diceButton, true);
        }
    }

    private void HandleMessage(string message)
    {
        AddMessage(message);
    }

    private void HandleDiceButtonClicked()
    {
        LeaveUpgradeMode();
        OnActionClicked.Invoke(MonopolyUIActionType.RollDiceOrNextTurn);
        SetInfo("已点击骰子选项\n请在 OnActionClicked 中接入掷骰或进入下一回合逻辑。");
    }

    private void RefreshAll()
    {
        if (ui.upgradeButtonText != null)
        {
            ui.upgradeButtonText.text = "升级选项\n点击后进入\n升级模式";
        }

        if (ui.diceButtonText != null)
        {
            ui.diceButtonText.text = "骰子选项\n点击后进入下一\n回合";
        }

        RefreshStats();
        SetInfo("信息栏\n例如使用升级功能时，点击相应建筑物/格子，会在这里先显示升级价格和效果。\n再次点击会在这里给个反馈。");
    }

    private void RefreshStats()
    {
        int money = currentPlayer == null ? fallbackMoney : currentPlayer.money;
        int income = currentPlayer == null ? fallbackIncome : CalculateCurrentPlayerIncome();

        if (ui.moneyText != null)
        {
            ui.moneyText.text = "金钱：" + money;
        }

        if (ui.incomeText != null)
        {
            ui.incomeText.text = "每回合收入：" + income;
        }
    }

    private int CalculateCurrentPlayerIncome()
    {
        if (currentPlayer == null || boardRegistry == null)
        {
            return fallbackIncome;
        }

        int income = 0;
        for (int i = 0; i < boardRegistry.Count; i++)
        {
            BoardGridView view = boardRegistry.GetView(i);
            if (view == null || view.RuntimeData == null || !view.RuntimeData.HasBuilding)
            {
                continue;
            }

            if (view.RuntimeData.ownerPlayerId == currentPlayer.playerId)
            {
                income += GridRules.GetTurnIncome(view.RuntimeData.buildingData);
            }
        }

        return income;
    }

    private string BuildGridInfo(BoardGridView gridView, bool asUpgradePreview)
    {
        GridData grid = gridView.RuntimeData;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("格子：" + gridView.GridIndex);

        if (grid == null)
        {
            builder.AppendLine("没有格子数据。");
            return builder.ToString().TrimEnd();
        }

        if (grid.kind == GridKind.Event || gridView.IsEventGrid)
        {
            builder.AppendLine("类型：事件格");
            builder.AppendLine("标记：" + gridView.gameObject.tag);
            return builder.ToString().TrimEnd();
        }

        builder.AppendLine("类型：建筑格");

        if (!grid.HasBuilding)
        {
            int buildCost = GridRules.GetBuildCost(BuildingType.ChainRestaurant);
            BuildingData preview = new BuildingData { buildingType = BuildingType.ChainRestaurant, level = 1 };
            builder.AppendLine("状态：空地");
            builder.AppendLine("建造价格：" + buildCost);
            builder.AppendLine("建造后每回合收入：" + GridRules.GetTurnIncome(preview));
            builder.AppendLine("建造后过路费：" + GridRules.GetPassFee(preview));
            return builder.ToString().TrimEnd();
        }

        BuildingData building = grid.buildingData;
        builder.AppendLine("建筑：" + GetBuildingName(building.buildingType));
        builder.AppendLine("等级：" + building.level);
        builder.AppendLine("所有者：" + GetOwnerLabel(grid.ownerPlayerId));
        builder.AppendLine("每回合收入：" + GridRules.GetTurnIncome(building));
        builder.AppendLine("过路费：" + GridRules.GetPassFee(building));

        if (building.IsMaxLevel)
        {
            builder.AppendLine("升级：已满级");
            return builder.ToString().TrimEnd();
        }

        int upgradeCost = GridRules.GetUpgradeCost(building.buildingType, building.level);
        BuildingData nextLevel = new BuildingData
        {
            buildingType = building.buildingType,
            level = building.level + 1
        };

        builder.AppendLine("升级价格：" + upgradeCost);
        builder.AppendLine("升级后每回合收入：" + GridRules.GetTurnIncome(nextLevel));
        builder.AppendLine("升级后过路费：" + GridRules.GetPassFee(nextLevel));

        if (asUpgradePreview && currentPlayer != null && currentPlayer.money < upgradeCost)
        {
            builder.AppendLine("资金不足：还差 " + (upgradeCost - currentPlayer.money));
        }

        return builder.ToString().TrimEnd();
    }

    private bool CanUpgradeGrid(BoardGridView gridView, out string blockedReason)
    {
        blockedReason = string.Empty;

        if (gridView == null || gridView.RuntimeData == null)
        {
            blockedReason = "无法升级：没有格子数据。";
            return false;
        }

        GridData grid = gridView.RuntimeData;
        if (!grid.HasBuilding)
        {
            blockedReason = "无法升级：这个格子还没有建筑。";
            return false;
        }

        if (grid.buildingData.IsMaxLevel)
        {
            blockedReason = "无法升级：建筑已经满级。";
            return false;
        }

        if (currentPlayer != null && grid.ownerPlayerId != currentPlayer.playerId)
        {
            blockedReason = "无法升级：只能升级当前玩家自己的建筑。";
            return false;
        }

        int upgradeCost = GridRules.GetUpgradeCost(grid.buildingData.buildingType, grid.buildingData.level);
        if (currentPlayer != null && currentPlayer.money < upgradeCost)
        {
            blockedReason = "无法升级：金钱不足，需要 " + upgradeCost + "。";
            return false;
        }

        return true;
    }

    private string GetOwnerLabel(int ownerPlayerId)
    {
        if (ownerPlayerId < 0)
        {
            return "无";
        }

        if (currentPlayer != null && ownerPlayerId == currentPlayer.playerId)
        {
            return currentPlayer.playerName;
        }

        return "玩家 " + ownerPlayerId;
    }

    private string GetBuildingName(BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.CrownRestaurant:
                return "皇冠餐厅";
            case BuildingType.FineRestaurant:
                return "精致餐厅";
            default:
                return "连锁餐厅";
        }
    }

    private void AddMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        recentMessages.Add(message);
        while (recentMessages.Count > 5)
        {
            recentMessages.RemoveAt(0);
        }
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    [SerializeField] private bool createBoardVisualizer = true;
    [SerializeField] private bool createBoardClicker = true;
    [SerializeField] private MonopolyHUDLayoutSettings layoutSettings;

    [Header("Board Visualizer Template")]
    [SerializeField] private MonopolyBoardBuildVisualizer visualizerTemplate;

    [Header("Building")]
    [SerializeField] private BuildingType defaultBuildType = BuildingType.ChainRestaurant;
    [SerializeField] private bool enableKeyboardActionShortcuts = false;

    [Header("Fallback Values")]
    [SerializeField] private int fallbackMoney;
    [SerializeField] private int fallbackIncome;

    [Header("Events")]
    public MonopolyUIActionEvent OnActionClicked = new MonopolyUIActionEvent();
    public BoardGridViewEvent OnUpgradeConfirmed = new BoardGridViewEvent();

    private readonly List<string> recentMessages = new List<string>();
    private MonopolyBoardBuildVisualizer boardVisualizer;
    private BoardGridInfoClicker boardClicker;
    private PlayerData currentPlayer;
    private OptionalActionContext lastOptionalContext;
    private BoardGridView selectedUpgradeGrid;
    private bool upgradeMode;
    private bool subscribed;
    private string pendingEventMessage;
    private Coroutine diceResultRoutine;

    public bool IsUpgradeMode => upgradeMode;
    public TurnStateMachine BoundStateMachine => stateMachine;
    public BoardGridRegistry BoundBoardRegistry => boardRegistry;
    public MonopolyHUDReferences References => ui;
    public PlayerData CurrentPlayer => currentPlayer;
    public OptionalActionContext LastOptionalContext => lastOptionalContext;

    private void Awake()
    {
        ResolveSceneReferences();

        if (createRuntimeUI && !ui.HasRequiredReferences())
        {
            ui = MonopolyHUDBuilder.Build("Monopoly HUD", layoutSettings);
        }

        EnsureBoardVisualizer();
        EnsureBoardClicker();
        ConfigureClickThroughUI();
        WireButtons();
        RefreshAll();
    }

    private void OnEnable()
    {
        Subscribe();
        EnsureBoardVisualizer();
        EnsureBoardClicker();
        ConfigureClickThroughUI();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!enableKeyboardActionShortcuts || stateMachine == null || stateMachine.CurrentState != TurnState.OptionalAction)
        {
            return;
        }

        if (upgradeMode)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryBuildChainRestaurant();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryBuildCrownRestaurant();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TryBuildFineRestaurant();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            stateMachine.RequestSkipOptionalAction();
            SetInfo("已跳过可选行动，进入下一回合。");
            RefreshBoardVisuals();
        }
    }

    public void BindTurnStateMachine(TurnStateMachine target)
    {
        if (stateMachine == target)
        {
            return;
        }

        Unsubscribe();
        stateMachine = target;
        Subscribe();
        EnsureBoardVisualizer();
        EnsureBoardClicker();
        RefreshStats();
    }

    public void BindBoardRegistry(BoardGridRegistry target)
    {
        boardRegistry = target;
        EnsureBoardVisualizer();
        EnsureBoardClicker();
        RefreshStats();
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

    public void SetBuildButtonInteractable(bool interactable)
    {
        SetButtonInteractable(ui.buildButton, interactable);
    }

    public void SetDiceButtonInteractable(bool interactable)
    {
        SetButtonInteractable(ui.diceButton, interactable);
    }

    public void SetUpgradeButtonInteractable(bool interactable)
    {
        SetButtonInteractable(ui.upgradeButton, interactable);
    }

    public void EnterUpgradeMode()
    {
        upgradeMode = true;
        selectedUpgradeGrid = null;
        OnActionClicked.Invoke(MonopolyUIActionType.EnterUpgradeMode);
        SetInfo("升级模式\n点击己方任意建筑查看升级费用和效果。\n再次点击同一建筑确认升级。");
        RefreshBoardVisuals();
    }

    public void LeaveUpgradeMode()
    {
        upgradeMode = false;
        selectedUpgradeGrid = null;
        RefreshBoardVisuals();
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
            if (TryUpgradeGrid(gridView))
            {
                OnActionClicked.Invoke(MonopolyUIActionType.ConfirmUpgrade);
                OnUpgradeConfirmed.Invoke(gridView);
                LeaveUpgradeMode();
            }

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

    public bool TryBuildChainRestaurant()
    {
        return TryBuildOnCurrentGrid(BuildingType.ChainRestaurant);
    }

    public bool TryBuildCrownRestaurant()
    {
        return TryBuildOnCurrentGrid(BuildingType.CrownRestaurant);
    }

    public bool TryBuildFineRestaurant()
    {
        return TryBuildOnCurrentGrid(BuildingType.FineRestaurant);
    }

    public bool TryBuildOnCurrentGrid(BuildingType buildingType)
    {
        ResolveSceneReferences();
        PlayerData player = GetActionPlayer();
        BoardGridView gridView = GetActionGridView(player);

        if (!CanBuildGrid(player, gridView, buildingType, out string blockedReason))
        {
            SetInfo(blockedReason);
            return false;
        }

        int buildCost = GridRules.GetBuildCost(buildingType);
        if (stateMachine != null && stateMachine.CurrentState == TurnState.OptionalAction)
        {
            bool success = stateMachine.RequestBuildOnCurrentGrid(buildingType);
            if (!success)
            {
                SetInfo("建造失败。");
                return false;
            }

            currentPlayer = player;
            RefreshStats();
            RefreshBoardVisuals();
            OnActionClicked.Invoke(MonopolyUIActionType.Build);
            SetInfo("建造成功\n格子：" + gridView.GridIndex + "\n建筑：" + GetBuildingName(buildingType) + "\n花费：" + buildCost);
            return true;
        }

        ChangeMoney(player, -buildCost);
        gridView.SetBuildingOwner(player, buildingType);

        if (!player.ownedGridIndexes.Contains(gridView.GridIndex))
        {
            player.ownedGridIndexes.Add(gridView.GridIndex);
        }

        currentPlayer = player;
        RefreshStats();
        RefreshBoardVisuals();
        OnActionClicked.Invoke(MonopolyUIActionType.Build);
        SetInfo("建造成功\n格子：" + gridView.GridIndex + "\n建筑：" + GetBuildingName(buildingType) + "\n花费：" + buildCost);
        return true;
    }

    public bool TryUpgradeGrid(BoardGridView gridView)
    {
        if (!CanUpgradeGrid(gridView, out string blockedReason))
        {
            SetInfo(blockedReason);
            return false;
        }

        PlayerData player = GetActionPlayer();
        BuildingData building = gridView.RuntimeData.buildingData;
        int oldLevel = building.level;
        int upgradeCost = GridRules.GetUpgradeCost(building.buildingType, building.level);

        if (stateMachine != null && stateMachine.CurrentState == TurnState.OptionalAction)
        {
            bool success = stateMachine.RequestUpgradeGrid(gridView);
            if (!success)
            {
                SetInfo("升级失败。");
                return false;
            }

            RefreshStats();
            RefreshBoardVisuals();
            SetInfo("升级成功\n格子：" + gridView.GridIndex + "\n建筑：" + GetBuildingName(building.buildingType) + "\n等级：" + oldLevel + " -> " + building.level + "\n花费：" + upgradeCost);
            return true;
        }

        ChangeMoney(player, -upgradeCost);
        gridView.UpgradeBuilding();
        RefreshStats();
        RefreshBoardVisuals();

        SetInfo("升级成功\n格子：" + gridView.GridIndex + "\n建筑：" + GetBuildingName(building.buildingType) + "\n等级：" + oldLevel + " -> " + building.level + "\n花费：" + upgradeCost);
        return true;
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

    private void EnsureBoardVisualizer()
    {
        if (!createBoardVisualizer)
        {
            return;
        }

        ResolveSceneReferences();

        if (boardVisualizer == null)
        {
            boardVisualizer = FindObjectOfType<MonopolyBoardBuildVisualizer>();
        }

        if (boardVisualizer == null)
        {
            GameObject visualizerObject = new GameObject("Monopoly Board Build Visualizer");
            boardVisualizer = visualizerObject.AddComponent<MonopolyBoardBuildVisualizer>();
        }

        if (visualizerTemplate != null)
        {
            boardVisualizer.CopyPrefabsFromTemplate(visualizerTemplate);
        }

        boardVisualizer.Bind(stateMachine, boardRegistry, this);
    }

    private void EnsureBoardClicker()
    {
        if (!createBoardClicker)
        {
            return;
        }

        if (boardClicker == null)
        {
            boardClicker = FindObjectOfType<BoardGridInfoClicker>();
        }

        if (boardClicker == null)
        {
            boardClicker = gameObject.AddComponent<BoardGridInfoClicker>();
        }

        boardClicker.Bind(this, Camera.main);
    }

    private void ConfigureClickThroughUI()
    {
        SetRaycastTarget(ui.statsPanel, false);
        SetRaycastTarget(ui.infoPanel, false);
        SetRaycastTarget(ui.moneyText, false);
        SetRaycastTarget(ui.turnText, false);
        SetRaycastTarget(ui.incomeText, false);
        SetRaycastTarget(ui.infoText, false);
        SetButtonRaycastTarget(ui.buildButton, true);
        SetButtonRaycastTarget(ui.upgradeButton, true);
        SetButtonRaycastTarget(ui.diceButton, true);
        SetRaycastTarget(ui.buildButtonText, false);
        SetRaycastTarget(ui.upgradeButtonText, false);
        SetRaycastTarget(ui.diceButtonText, false);
    }

    private void RefreshBoardVisuals()
    {
        if (boardVisualizer != null)
        {
            boardVisualizer.RefreshNow();
        }
    }

    private void WireButtons()
    {
        if (ui.buildButton != null)
        {
            ui.buildButton.onClick.RemoveListener(HandleBuildButtonClicked);
            ui.buildButton.onClick.AddListener(HandleBuildButtonClicked);
        }

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
        stateMachine.OnDiceRolled += HandleDiceRolled;
        stateMachine.OnStateChanged += HandleStateChanged;
        stateMachine.OnLevelChanged += HandleLevelChanged;
        stateMachine.OnMessage += HandleMessage;
        stateMachine.OnEventMessage += HandleEventMessage;
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
        stateMachine.OnDiceRolled -= HandleDiceRolled;
        stateMachine.OnStateChanged -= HandleStateChanged;
        stateMachine.OnLevelChanged -= HandleLevelChanged;
        stateMachine.OnMessage -= HandleMessage;
        stateMachine.OnEventMessage -= HandleEventMessage;
        subscribed = false;
    }

    private void HandlePlayerChanged(PlayerData player)
    {
        currentPlayer = player;
        RefreshStats();
        RefreshBoardVisuals();
    }

    private void HandleMoneyChanged(PlayerData player, int money)
    {
        if (currentPlayer == null || player == currentPlayer || player.playerId == currentPlayer.playerId)
        {
            currentPlayer = player;
            RefreshStats();
            RefreshBoardVisuals();
        }
    }

    private void HandleGridResolved(PlayerData player, GridData grid)
    {
        if (grid != null)
        {
            AddMessage(player.playerName + " 到达格子 " + grid.index);
        }

        RefreshBoardVisuals();
    }

    private void HandleOptionalActionRequested(OptionalActionContext context)
    {
        lastOptionalContext = context;
        currentPlayer = context == null ? currentPlayer : context.currentPlayer;
        RefreshStats();
        RefreshBoardVisuals();

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
            builder.AppendLine("当前格子可以建造：");
            AppendBuildOptions(builder);
            builder.AppendLine("快捷键：1/2/3 建造，0 跳过。");
        }

        if (context.canUpgrade)
        {
            builder.AppendLine("可升级建筑：" + context.upgradableGrids.Count + " 个");
            AppendCheapestUpgrade(builder, context);
        }

        if (!context.canBuild && !context.canUpgrade)
        {
            builder.AppendLine("当前没有可选建筑操作。");
        }

        if (!string.IsNullOrEmpty(pendingEventMessage))
        {
            builder.AppendLine();
            builder.Append(pendingEventMessage);
            pendingEventMessage = null;
        }

        SetInfo(builder.ToString().TrimEnd());
    }

    private void HandleStateChanged(TurnState state)
    {
        RefreshStats();

        if (state == TurnState.GameOver)
        {
            SetButtonInteractable(ui.buildButton, false);
            SetButtonInteractable(ui.upgradeButton, false);
            SetButtonInteractable(ui.diceButton, false);
            SetInfo("游戏结束");
            RefreshBoardVisuals();
            return;
        }

        SetButtonInteractable(ui.buildButton, state == TurnState.OptionalAction);
        SetButtonInteractable(ui.upgradeButton, state == TurnState.OptionalAction);
        SetButtonInteractable(ui.diceButton, state == TurnState.TurnStart || state == TurnState.OptionalAction);
        RefreshBoardVisuals();
    }

    private void HandleLevelChanged(int levelIndex)
    {
        RefreshStats();
    }

    private void HandleMessage(string message)
    {
        AddMessage(message);
    }

    private void HandleEventMessage(string message)
    {
        pendingEventMessage = message;
    }

    private void HandleDiceRolled(PlayerData player, int value)
    {
        if (diceResultRoutine != null)
        {
            StopCoroutine(diceResultRoutine);
        }

        diceResultRoutine = StartCoroutine(ShowDiceResultRoutine(value));
    }

    private IEnumerator ShowDiceResultRoutine(int value)
    {
        if (ui.diceButtonText == null)
        {
            yield break;
        }

        string previousText = ui.diceButtonText.text;
        ui.diceButtonText.text = "\u9AB0\u5B50\uFF1A" + value;
        yield return new WaitForSeconds(1f);
        ui.diceButtonText.text = previousText;
        diceResultRoutine = null;
    }

    private void HandleBuildButtonClicked()
    {
        LeaveUpgradeMode();
        TryBuildOnCurrentGrid(defaultBuildType);
    }

    private void HandleDiceButtonClicked()
    {
        LeaveUpgradeMode();
        ResolveSceneReferences();

        if (stateMachine == null)
        {
            stateMachine = FindSceneObjectIncludingInactive<TurnStateMachine>();
        }

        if (stateMachine == null)
        {
            Debug.LogWarning("[MonopolyHUD] TurnStateMachine not found in loaded scenes.");
            SetInfo("未找到回合状态机，无法开始当前回合。");
            return;
        }

        if (stateMachine.CurrentState == TurnState.OptionalAction)
        {
            stateMachine.RequestSkipOptionalAction();
            OnActionClicked.Invoke(MonopolyUIActionType.RollDiceOrNextTurn);
            SetInfo("已跳过可选行动，进入下一回合。");
            RefreshBoardVisuals();
            return;
        }

        if (stateMachine.CurrentState != TurnState.TurnStart)
        {
            SetInfo("当前还不能掷骰，请等待回合开始。");
            return;
        }

        stateMachine.RequestTurnStart();
        OnActionClicked.Invoke(MonopolyUIActionType.RollDiceOrNextTurn);
        SetInfo("已开始当前回合，正在掷骰。");
    }

    private T FindSceneObjectIncludingInactive<T>() where T : Component
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < objects.Length; i++)
        {
            T candidate = objects[i];
            if (candidate != null && candidate.gameObject != null && candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private void RefreshAll()
    {
        if (ui.buildButtonText != null)
        {
            ui.buildButtonText.text = "建造\n默认：" + GetBuildingName(defaultBuildType) + " " + GridRules.GetBuildCost(defaultBuildType)
                + "\n1/2/3 可选";
        }

        if (ui.upgradeButtonText != null)
        {
            ui.upgradeButtonText.text = "升级\n点击后选择\n己方建筑";
        }

        if (ui.diceButtonText != null)
        {
            ui.diceButtonText.text = "掷骰 / 跳过\n回合开始掷骰\n可选行动跳过";
        }

        RefreshStats();
        SetInfo("信息栏\n绿色格子：当前可建造。\n灰色格子：建筑格，但当前不能建造。\n蓝色建筑：己方，红色建筑：对方，黄色格子：事件格。\n建造费用：连锁 12 / 皇冠 20 / 精致 18。");
        RefreshBoardVisuals();
    }

    private void RefreshStats()
    {
        PlayerData player = GetActionPlayer();
        int money = player == null ? fallbackMoney : player.money;
        int income = player == null ? fallbackIncome : CalculateCurrentPlayerIncome(player);
        int currentTurn = stateMachine == null ? 0 : stateMachine.CurrentTurnInLevel;
        int turnsPerLevel = stateMachine == null ? 0 : stateMachine.TurnsPerLevel;
        int displayTurn = turnsPerLevel > 0
            ? Mathf.Clamp(currentTurn + 1, 1, turnsPerLevel)
            : currentTurn + 1;

        if (ui.moneyText != null)
        {
            ui.moneyText.text = "\u91D1\u94B1\uFF1A" + money;
        }

        if (ui.turnText != null)
        {
            ui.turnText.text = turnsPerLevel > 0
                ? "\u56DE\u5408\uFF1A" + displayTurn + "/" + turnsPerLevel
                : "\u56DE\u5408\uFF1A" + displayTurn;
        }

        if (ui.incomeText != null)
        {
            ui.incomeText.text = "\u6BCF\u56DE\u5408\u6536\u5165\uFF1A" + income;
        }
    }

    private int CalculateCurrentPlayerIncome(PlayerData player)
    {
        if (player == null || boardRegistry == null)
        {
            return fallbackIncome;
        }

        int income = 0;
        for (int i = 0; i < boardRegistry.Count; i++)
        {
            BoardGridView view = boardRegistry.GetView(i);
            if (view != null && view.RuntimeData != null && view.RuntimeData.HasBuilding && view.RuntimeData.ownerPlayerId == player.playerId)
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
            builder.AppendLine("暂未实现具体事件效果。");
            return builder.ToString().TrimEnd();
        }

        builder.AppendLine("类型：建筑格");

        if (!grid.HasBuilding)
        {
            builder.AppendLine("当前没有建筑。");
            AppendBuildOptions(builder);
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

        PlayerData player = GetActionPlayer();
        if (asUpgradePreview && player != null && player.money < upgradeCost)
        {
            builder.AppendLine("资金不足：还差 " + (upgradeCost - player.money));
        }

        return builder.ToString().TrimEnd();
    }

    private bool CanBuildGrid(PlayerData player, BoardGridView gridView, BuildingType buildingType, out string blockedReason)
    {
        blockedReason = string.Empty;

        if (player == null)
        {
            blockedReason = "无法建造：没有当前玩家。";
            return false;
        }

        if (gridView == null || gridView.RuntimeData == null)
        {
            blockedReason = "无法建造：没有当前落点格子。";
            return false;
        }

        if (!gridView.IsBuildingGrid || gridView.RuntimeData.kind != GridKind.Building)
        {
            blockedReason = "无法建造：只有建筑格可以建造。";
            return false;
        }

        if (gridView.RuntimeData.HasBuilding)
        {
            blockedReason = "无法建造：这个格子已经有建筑。";
            return false;
        }

        int buildCost = GridRules.GetBuildCost(buildingType);
        if (player.money < buildCost)
        {
            blockedReason = "无法建造：金钱不足，需要 " + buildCost + "。";
            return false;
        }

        return true;
    }

    private bool CanUpgradeGrid(BoardGridView gridView, out string blockedReason)
    {
        blockedReason = string.Empty;
        PlayerData player = GetActionPlayer();

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

        if (player != null && grid.ownerPlayerId != player.playerId)
        {
            blockedReason = "无法升级：只能升级当前玩家自己的建筑。";
            return false;
        }

        int upgradeCost = GridRules.GetUpgradeCost(grid.buildingData.buildingType, grid.buildingData.level);
        if (player != null && player.money < upgradeCost)
        {
            blockedReason = "无法升级：金钱不足，需要 " + upgradeCost + "。";
            return false;
        }

        return true;
    }

    private PlayerData GetActionPlayer()
    {
        if (lastOptionalContext != null && lastOptionalContext.currentPlayer != null)
        {
            return lastOptionalContext.currentPlayer;
        }

        if (currentPlayer != null)
        {
            return currentPlayer;
        }

        if (stateMachine != null)
        {
            List<PlayerData> players = stateMachine.GetAllPlayers();
            if (players != null && players.Count > 0)
            {
                int index = Mathf.Clamp(stateMachine.CurrentPlayerIndex, 0, players.Count - 1);
                return players[index];
            }
        }

        return null;
    }

    private BoardGridView GetActionGridView(PlayerData player)
    {
        if (lastOptionalContext != null && lastOptionalContext.currentGridView != null)
        {
            return lastOptionalContext.currentGridView;
        }

        if (player != null && boardRegistry != null)
        {
            return boardRegistry.GetView(player.position);
        }

        return null;
    }

    private void ChangeMoney(PlayerData player, int delta)
    {
        if (player == null)
        {
            return;
        }

        if (stateMachine != null)
        {
            MethodInfo changeMoneyMethod = typeof(TurnStateMachine).GetMethod("ChangeMoney", BindingFlags.Instance | BindingFlags.NonPublic);
            if (changeMoneyMethod != null)
            {
                changeMoneyMethod.Invoke(stateMachine, new object[] { player, delta });
                return;
            }
        }

        player.money += delta;
        RefreshStats();
    }

    private void AppendBuildOptions(StringBuilder builder)
    {
        AppendBuildOption(builder, BuildingType.ChainRestaurant);
        AppendBuildOption(builder, BuildingType.CrownRestaurant);
        AppendBuildOption(builder, BuildingType.FineRestaurant);
    }

    private void AppendBuildOption(StringBuilder builder, BuildingType buildingType)
    {
        BuildingData preview = new BuildingData { buildingType = buildingType, level = 1 };
        builder.AppendLine(GetBuildingName(buildingType) + "：建造 " + GridRules.GetBuildCost(buildingType)
            + " / 收益 " + GridRules.GetTurnIncome(preview)
            + " / 过路费 " + GridRules.GetPassFee(preview));
    }

    private void AppendCheapestUpgrade(StringBuilder builder, OptionalActionContext context)
    {
        if (context == null || context.upgradableGrids == null || context.upgradableGrids.Count == 0)
        {
            return;
        }

        int cheapestCost = int.MaxValue;
        GridData cheapestGrid = null;

        for (int i = 0; i < context.upgradableGrids.Count; i++)
        {
            GridData grid = context.upgradableGrids[i];
            if (grid == null || !grid.HasBuilding || grid.buildingData.IsMaxLevel)
            {
                continue;
            }

            int cost = GridRules.GetUpgradeCost(grid.buildingData.buildingType, grid.buildingData.level);
            if (cost > 0 && cost < cheapestCost)
            {
                cheapestCost = cost;
                cheapestGrid = grid;
            }
        }

        if (cheapestGrid != null)
        {
            builder.AppendLine("最低升级费用：" + cheapestCost + "（格子 " + cheapestGrid.index + "）");
        }
    }

    private string GetOwnerLabel(int ownerPlayerId)
    {
        if (ownerPlayerId < 0)
        {
            return "无";
        }

        PlayerData player = GetActionPlayer();
        if (player != null && ownerPlayerId == player.playerId)
        {
            return player.playerName;
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

    private void SetRaycastTarget(Graphic graphic, bool raycastTarget)
    {
        if (graphic != null)
        {
            graphic.raycastTarget = raycastTarget;
        }
    }

    private void SetRaycastTarget(RectTransform rectTransform, bool raycastTarget)
    {
        if (rectTransform == null)
        {
            return;
        }

        Graphic graphic = rectTransform.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = raycastTarget;
        }
    }

    private void SetButtonRaycastTarget(Button button, bool raycastTarget)
    {
        if (button == null)
        {
            return;
        }

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = raycastTarget;
        }
    }
}

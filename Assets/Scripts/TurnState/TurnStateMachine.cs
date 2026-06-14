using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OptionalActionContext
{
    public PlayerData currentPlayer;
    public GridData currentGrid;
    public List<GridData> upgradableGrids = new List<GridData>();
    public bool canBuild;
    public bool canUpgrade;
}

[Serializable]
public class StateMachineSettings
{
    [Range(2, 8)] public int playerCount = 2;
    [Range(1, 10)] public int levelCount = 2;
    [Range(1, 100)] public int turnsPerLevel = 20;
    [Range(10, 9999)] public int initialMoney = 1000;
    [Range(1, 100)] public int startReward = 200;
    public bool carryMoneyBetweenLevels = false;
}

public class TurnStateMachine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private StateMachineSettings settings = new StateMachineSettings();

    [Header("Runtime")]
    [SerializeField] private List<PlayerData> players = new List<PlayerData>();
    [SerializeField] private List<GridData> boardGrids = new List<GridData>();

    [Header("Optional")]
    [SerializeField] private bool autoPlayAllPlayers = true;
    [SerializeField] private float moveStepDelay = 0.12f;

    public TurnState CurrentState { get; private set; } = TurnState.Idle;
    public int CurrentLevelIndex { get; private set; }
    public int CurrentTurnInLevel { get; private set; }
    public int CurrentPlayerIndex { get; private set; }
    public int LastDiceValue { get; private set; }

    public event Action<TurnState> OnStateChanged;
    public event Action<int> OnLevelChanged;
    public event Action<PlayerData> OnPlayerChanged;
    public event Action<PlayerData, int> OnMoneyChanged;
    public event Action<PlayerData, GridData> OnGridResolved;
    public event Action<OptionalActionContext> OnOptionalActionRequested;
    public event Action<string> OnMessage;

    private bool isInitialized;
    private bool isBusy;

    private void Start()
    {
        InitializeGame();
        EnterState(TurnState.TurnStart);
    }

    private void InitializeGame()
    {
        if (isInitialized)
        {
            return;
        }

        CreatePlayersIfNeeded();
        CreateBoardIfNeeded();

        CurrentLevelIndex = 0;
        CurrentTurnInLevel = 0;
        CurrentPlayerIndex = 0;
        isInitialized = true;

        BroadcastMessage("Turn state machine initialized.");
    }

    private void CreatePlayersIfNeeded()
    {
        if (players == null)
        {
            players = new List<PlayerData>();
        }

        if (players.Count > 0)
        {
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

        for (int i = 0; i < settings.playerCount; i++)
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

    private void CreateBoardIfNeeded()
    {
        if (boardGrids == null)
        {
            boardGrids = new List<GridData>();
        }

        if (boardGrids.Count == 0)
        {
            BuildDefaultBoard();
        }
    }

    private void BuildDefaultBoard()
    {
        boardGrids.Clear();

        List<GridKind> kinds = new List<GridKind>();
        for (int i = 0; i < 21; i++)
        {
            kinds.Add(GridKind.Building);
        }

        for (int i = 0; i < 15; i++)
        {
            kinds.Add(GridKind.Event);
        }

        ShuffleKinds(kinds);

        for (int i = 0; i < kinds.Count; i++)
        {
            GridData Grid = new GridData();
            Grid.index = i;
            Grid.kind = kinds[i];
            Grid.ownerPlayerId = -1;
            Grid.buildingData = null;
            boardGrids.Add(Grid);
        }
    }

    private void ShuffleKinds(List<GridKind> kinds)
    {
        for (int i = 0; i < kinds.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, kinds.Count);
            GridKind temp = kinds[i];
            kinds[i] = kinds[randomIndex];
            kinds[randomIndex] = temp;
        }
    }

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
                BroadcastMessage("Game over.");
                break;
        }
    }

    private void HandleTurnStart()
    {
        if (isBusy)
        {
            return;
        }

        if (GetCurrentPlayer().IsBankrupt)
        {
            EnterState(TurnState.NextPlayer);
            return;
        }

        BroadcastMessage("Turn start: " + GetCurrentPlayer().playerName);
        OnPlayerChanged?.Invoke(GetCurrentPlayer());
        EnterState(TurnState.RollDice);
    }

    private void HandleRollDice()
    {
        LastDiceValue = UnityEngine.Random.Range(1, 7);
        BroadcastMessage(GetCurrentPlayer().playerName + " rolled " + LastDiceValue);
        EnterState(TurnState.Move);
    }

    private void HandleMove()
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
        for (int i = 0; i < LastDiceValue; i++)
        {
            player.position = (player.position + 1) % GetBoardSize();
            BroadcastMessage(player.playerName + " moved to Grid " + player.position);

            if (player.position == 0)
            {
                ChangeMoney(player, settings.startReward);
                BroadcastMessage(player.playerName + " passed start and gained " + settings.startReward);
            }

            yield return new WaitForSeconds(moveStepDelay);
        }

        isBusy = false;
        EnterState(TurnState.ResolveGrid);
    }

    private void HandleResolveGrid()
    {
        GridData Grid = GetCurrentGrid();
        PlayerData player = GetCurrentPlayer();

        if (Grid.kind == GridKind.Building)
        {
            BroadcastMessage(player.playerName + " landed on building Grid " + Grid.index);

            if (Grid.HasBuilding && Grid.ownerPlayerId >= 0 && Grid.ownerPlayerId != player.playerId)
            {
                ResolvePassFee(player, Grid);
            }

            OnGridResolved?.Invoke(player, Grid);
        }
        else
        {
            BroadcastMessage(player.playerName + " landed on event Grid " + Grid.index);
            OnGridResolved?.Invoke(player, Grid);
            TriggerEventGrid(Grid, player);
        }

        EnterState(TurnState.OptionalAction);
    }

    private void HandleOptionalAction()
    {
        OptionalActionContext context = BuildOptionalActionContext();
        OnOptionalActionRequested?.Invoke(context);

        OptionalActionType action = DecideOptionalAction(context);

        if (action == OptionalActionType.Build)
        {
            TryBuildOnCurrentGrid(context.currentPlayer, context.currentGrid);
        }
        else if (action == OptionalActionType.Upgrade)
        {
            TryUpgradeGlobalBuilding(context.currentPlayer);
        }
        else
        {
            BroadcastMessage(context.currentPlayer.playerName + " skipped optional action.");
        }

        EnterState(TurnState.EndTurn);
    }

    private void HandleEndTurn()
    {
        PlayerData player = GetCurrentPlayer();
        SettleTurnIncome(player);
        CurrentTurnInLevel++;
        EnterState(TurnState.CheckLevelEnd);
    }

    private void HandleNextPlayer()
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % players.Count;
        OnPlayerChanged?.Invoke(GetCurrentPlayer());
        EnterState(TurnState.TurnStart);
    }

    private void HandleCheckLevelEnd()
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

    private void ResetForNextLevel()
    {
        CurrentTurnInLevel = 0;
        CurrentPlayerIndex = 0;
        BuildDefaultBoard();

        for (int i = 0; i < players.Count; i++)
        {
            players[i].position = 0;
            players[i].ownedGridIndexes.Clear();

            if (!settings.carryMoneyBetweenLevels)
            {
                players[i].money = settings.initialMoney;
            }
        }

        BroadcastMessage("Level " + (CurrentLevelIndex + 1) + " started.");
    }

    private PlayerData GetCurrentPlayer()
    {
        if (players == null || players.Count == 0)
        {
            return null;
        }

        return players[CurrentPlayerIndex % players.Count];
    }

    private GridData GetCurrentGrid()
    {
        int index = GetCurrentPlayer().position;
        if (boardGrids == null || boardGrids.Count == 0)
        {
            return null;
        }

        return boardGrids[index % boardGrids.Count];
    }

    private int GetBoardSize()
    {
        if (boardGrids == null || boardGrids.Count == 0)
        {
            return 36;
        }

        return boardGrids.Count;
    }

    private OptionalActionContext BuildOptionalActionContext()
    {
        OptionalActionContext context = new OptionalActionContext();
        context.currentPlayer = GetCurrentPlayer();
        context.currentGrid = GetCurrentGrid();
        context.canBuild = CanBuildOnCurrentGrid(context.currentPlayer, context.currentGrid);
        context.upgradableGrids = GetUpgradableGrids(context.currentPlayer);
        context.canUpgrade = context.upgradableGrids.Count > 0;
        return context;
    }

    private OptionalActionType DecideOptionalAction(OptionalActionContext context)
    {
        if (context == null || context.currentPlayer == null || context.currentGrid == null)
        {
            return OptionalActionType.Skip;
        }

        if (context.currentPlayer.playerKind == PlayerKind.Enemy || autoPlayAllPlayers)
        {
            if (context.canBuild && CanAffordBuild(context.currentPlayer, context.currentGrid))
            {
                return OptionalActionType.Build;
            }

            if (context.canUpgrade && CanAffordAnyUpgrade(context.currentPlayer, context.upgradableGrids))
            {
                return OptionalActionType.Upgrade;
            }

            return OptionalActionType.Skip;
        }

        if (context.canBuild && CanAffordBuild(context.currentPlayer, context.currentGrid))
        {
            return OptionalActionType.Build;
        }

        if (context.canUpgrade && CanAffordAnyUpgrade(context.currentPlayer, context.upgradableGrids))
        {
            return OptionalActionType.Upgrade;
        }

        return OptionalActionType.Skip;
    }

    private bool CanBuildOnCurrentGrid(PlayerData player, GridData Grid)
    {
        if (player == null || Grid == null)
        {
            return false;
        }

        return Grid.kind == GridKind.Building && !Grid.HasBuilding;
    }

    private void ResolvePassFee(PlayerData currentPlayer, GridData Grid)
    {
        PlayerData owner = GetPlayerById(Grid.ownerPlayerId);
        if (owner == null || Grid.buildingData == null)
        {
            return;
        }

        int fee = GetPassFee(Grid.buildingData);
        if (fee <= 0)
        {
            return;
        }

        int paidFee = Mathf.Min(currentPlayer.money, fee);
        ChangeMoney(currentPlayer, -paidFee);
        ChangeMoney(owner, paidFee);

        BroadcastMessage(currentPlayer.playerName + " paid pass fee " + paidFee + " to " + owner.playerName);
    }

    private bool CanAffordBuild(PlayerData player, GridData Grid)
    {
        if (!CanBuildOnCurrentGrid(player, Grid))
        {
            return false;
        }

        return player.money >= GetBuildCost(BuildingType.ChainRestaurant);
    }

    private bool CanAffordAnyUpgrade(PlayerData player, List<GridData> Grids)
    {
        if (player == null || Grids == null)
        {
            return false;
        }

        for (int i = 0; i < Grids.Count; i++)
        {
            GridData Grid = Grids[i];
            if (Grid != null && Grid.HasBuilding && Grid.ownerPlayerId == player.playerId)
            {
                int upgradeCost = GetUpgradeCost(Grid.buildingData.level);
                if (player.money >= upgradeCost)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void TryBuildOnCurrentGrid(PlayerData player, GridData Grid)
    {
        if (!CanBuildOnCurrentGrid(player, Grid))
        {
            BroadcastMessage("Build failed.");
            return;
        }

        int buildCost = GetBuildCost(BuildingType.ChainRestaurant);
        if (player.money < buildCost)
        {
            BroadcastMessage(player.playerName + " has not enough money to build.");
            return;
        }

        ChangeMoney(player, -buildCost);
        Grid.buildingData = new BuildingData();
        Grid.buildingData.buildingType = BuildingType.ChainRestaurant;
        Grid.buildingData.level = 1;
        Grid.ownerPlayerId = player.playerId;

        if (!player.ownedGridIndexes.Contains(Grid.index))
        {
            player.ownedGridIndexes.Add(Grid.index);
        }

        BroadcastMessage(player.playerName + " built a ChainRestaurant at Grid " + Grid.index);
    }

    private void TryUpgradeGlobalBuilding(PlayerData player)
    {
        GridData target = FindLowestLevelUpgradableGrid(player);
        if (target == null)
        {
            BroadcastMessage("No upgrade target found.");
            return;
        }

        int upgradeCost = GetUpgradeCost(target.buildingData.level);
        if (player.money < upgradeCost)
        {
            BroadcastMessage(player.playerName + " has not enough money to upgrade.");
            return;
        }

        ChangeMoney(player, -upgradeCost);
        target.buildingData.level = Mathf.Min(target.buildingData.level + 1, 3);
        BroadcastMessage(player.playerName + " upgraded Grid " + target.index + " to level " + target.buildingData.level);
    }

    private GridData FindLowestLevelUpgradableGrid(PlayerData player)
    {
        GridData bestGrid = null;
        for (int i = 0; i < boardGrids.Count; i++)
        {
            GridData Grid = boardGrids[i];
            if (Grid == null || !Grid.HasBuilding)
            {
                continue;
            }

            if (Grid.ownerPlayerId != player.playerId)
            {
                continue;
            }

            if (Grid.buildingData.IsMaxLevel)
            {
                continue;
            }

            if (bestGrid == null)
            {
                bestGrid = Grid;
                continue;
            }

            if (Grid.buildingData.level < bestGrid.buildingData.level)
            {
                bestGrid = Grid;
            }
        }

        return bestGrid;
    }

    private List<GridData> GetUpgradableGrids(PlayerData player)
    {
        List<GridData> result = new List<GridData>();
        for (int i = 0; i < boardGrids.Count; i++)
        {
            GridData Grid = boardGrids[i];
            if (Grid == null || !Grid.HasBuilding)
            {
                continue;
            }

            if (Grid.ownerPlayerId == player.playerId && !Grid.buildingData.IsMaxLevel)
            {
                result.Add(Grid);
            }
        }

        return result;
    }

    private void TriggerEventGrid(GridData Grid, PlayerData player)
    {
        BroadcastMessage("Event framework placeholder triggered at Grid " + Grid.index + " for " + player.playerName);
    }

    private void SettleTurnIncome(PlayerData player)
    {
        int income = 0;
        for (int i = 0; i < boardGrids.Count; i++)
        {
            GridData Grid = boardGrids[i];
            if (Grid == null || !Grid.HasBuilding)
            {
                continue;
            }

            if (Grid.ownerPlayerId == player.playerId)
            {
                income += GetTurnIncome(Grid.buildingData);
            }
        }

        if (income > 0)
        {
            ChangeMoney(player, income);
            BroadcastMessage(player.playerName + " gained turn income: " + income);
        }
    }

    private int GetTurnIncome(BuildingData buildingData)
    {
        if (buildingData == null)
        {
            return 0;
        }

        int baseIncome = 20;
        switch (buildingData.buildingType)
        {
            case BuildingType.ChainRestaurant:
                baseIncome = 15;
                break;
            case BuildingType.CrownRestaurant:
                baseIncome = 25;
                break;
            case BuildingType.FineRestaurant:
                baseIncome = 35;
                break;
        }

        return baseIncome * buildingData.level;
    }

    private int GetPassFee(BuildingData buildingData)
    {
        if (buildingData == null)
        {
            return 0;
        }

        int baseFee = 30;
        switch (buildingData.buildingType)
        {
            case BuildingType.ChainRestaurant:
                baseFee = 20;
                break;
            case BuildingType.CrownRestaurant:
                baseFee = 40;
                break;
            case BuildingType.FineRestaurant:
                baseFee = 60;
                break;
        }

        return baseFee * buildingData.level;
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

    private int GetBuildCost(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.CrownRestaurant:
                return 250;
            case BuildingType.FineRestaurant:
                return 400;
            default:
                return 150;
        }
    }

    private int GetUpgradeCost(int currentLevel)
    {
        switch (currentLevel)
        {
            case 1:
                return 100;
            case 2:
                return 180;
            default:
                return 0;
        }
    }

    private void BroadcastMessage(string message)
    {
        Debug.Log("[TurnStateMachine] " + message);
        OnMessage?.Invoke(message);
    }
}
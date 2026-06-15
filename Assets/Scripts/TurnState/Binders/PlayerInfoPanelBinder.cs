using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 玩家信息面板绑定器。
// 显示每个玩家的金钱和每回合收入。
public class PlayerInfoPanelBinder : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private TurnStateMachine turnStateMachine;

    [Header("Player Info Displays")]
    [SerializeField] private List<PlayerInfoEntry> entries = new List<PlayerInfoEntry>();

    private void Awake()
    {
        if (turnStateMachine == null)
        {
            turnStateMachine = FindObjectOfType<TurnStateMachine>();
        }
    }

    private void Start()
    {
        RefreshAll();
    }

    private void OnEnable()
    {
        if (turnStateMachine != null)
        {
            turnStateMachine.OnMoneyChanged += HandleMoneyChanged;
            turnStateMachine.OnPlayerChanged += HandlePlayerChanged;
            turnStateMachine.OnGridResolved += HandleGridResolved;
            turnStateMachine.OnLevelChanged += HandleLevelChanged;
        }
    }

    private void OnDisable()
    {
        if (turnStateMachine != null)
        {
            turnStateMachine.OnMoneyChanged -= HandleMoneyChanged;
            turnStateMachine.OnPlayerChanged -= HandlePlayerChanged;
            turnStateMachine.OnGridResolved -= HandleGridResolved;
            turnStateMachine.OnLevelChanged -= HandleLevelChanged;
        }
    }

    private void HandleMoneyChanged(PlayerData player, int newMoney)
    {
        RefreshEntry(player);
    }

    private void HandlePlayerChanged(PlayerData player)
    {
        RefreshEntry(player);
    }

    private void HandleGridResolved(PlayerData player, GridData grid)
    {
        // 格子结算后可能改变建筑归属，刷新收入
        RefreshEntry(player);
    }

    private void HandleLevelChanged(int levelIndex)
    {
        RefreshAll();
    }

    // 刷新所有玩家条目
    public void RefreshAll()
    {
        if (turnStateMachine == null)
        {
            return;
        }

        List<PlayerData> allPlayers = turnStateMachine.GetAllPlayers();
        if (allPlayers == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == null)
            {
                continue;
            }

            // 找到对应 playerId 的玩家数据
            PlayerData matched = null;
            for (int j = 0; j < allPlayers.Count; j++)
            {
                if (allPlayers[j] != null && allPlayers[j].playerId == entries[i].playerId)
                {
                    matched = allPlayers[j];
                    break;
                }
            }

            if (matched != null)
            {
                entries[i].Refresh(matched, CalculateTurnIncome(matched));
            }
        }
    }

    // 根据玩家数据刷新对应条目
    private void RefreshEntry(PlayerData player)
    {
        if (player == null)
        {
            return;
        }

        // 按 playerId 匹配条目
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].playerId == player.playerId)
            {
                entries[i].Refresh(player, CalculateTurnIncome(player));
                return;
            }
        }

        // 没有精确匹配时按列表索引
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].isGenericEntry)
            {
                entries[i].Refresh(player, CalculateTurnIncome(player));
                return;
            }
        }
    }

    private void RefreshEntryByPlayerId(int playerId)
    {
        if (turnStateMachine == null)
        {
            return;
        }

        PlayerData player = turnStateMachine.GetPlayer(playerId);
        if (player == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].playerId == playerId)
            {
                entries[i].Refresh(player, CalculateTurnIncome(player));
                return;
            }
        }
    }

    // 计算指定玩家的每回合收入
    private int CalculateTurnIncome(PlayerData player)
    {
        if (player == null)
        {
            return 0;
        }

        BoardGridRegistry registry = GetBoardRegistry();
        if (registry == null)
        {
            return 0;
        }

        int income = 0;
        for (int i = 0; i < registry.Count; i++)
        {
            BoardGridView view = registry.GetView(i);
            if (view == null || !view.RuntimeData.HasBuilding)
            {
                continue;
            }

            if (view.RuntimeData.ownerPlayerId == player.playerId)
            {
                income += GridRules.GetTurnIncome(view.RuntimeData.buildingData);
            }
        }

        return income;
    }

    private BoardGridRegistry GetBoardRegistry()
    {
        return FindObjectOfType<BoardGridRegistry>();
    }

    // 每个玩家的一条信息显示条目
    [System.Serializable]
    public class PlayerInfoEntry
    {
        public int playerId;           // 匹配的玩家 ID
        public bool isGenericEntry;    // 是否没有精确 ID 匹配（用第一个未匹配的条目）
        public TMP_Text nameText;      // 玩家名称
        public TMP_Text moneyText;     // 金钱
        public TMP_Text incomeText;    // 每回合收入

        public void Refresh(PlayerData player, int turnIncome)
        {
            if (player == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = "money";
            }

            if (moneyText != null)
            {
                moneyText.text = player.money.ToString();
            }

            if (incomeText != null)
            {
                incomeText.text = turnIncome.ToString();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 事件格子处理器。
/// Event grid resolver.
/// 从 TurnStateMachine.ResolveEventGrid() 中调用，负责随机选取事件并结算效果。
/// </summary>
public static class EventGridResolver
{
    /// <summary>
    /// 处理玩家踏入事件格子。
    /// Resolve event when player steps on an event grid.
    /// </summary>
    /// <param name="view">当前事件格子视图</param>
    /// <param name="currentPlayer">当前玩家</param>
    /// <param name="allPlayers">所有玩家列表</param>
    /// <param name="boardRegistry">地图格子登记表</param>
    /// <param name="logMessage">输出消息的回调</param>
    /// <param name="changeMoney">修改金钱的回调 (player, delta)</param>
    /// <param name="getPlayerMover">根据 playerId 获取 PlayerTokenMover 的回调，用于顺风车移动</param>
    /// <param name="onPostMoveResolve">顺风车移动后结算新格子的回调 (player, gridView) —— 仅处理过路费，不触发新事件</param>
    public static void Resolve(
        BoardGridView view,
        PlayerData currentPlayer,
        List<PlayerData> allPlayers,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage,
        Action<PlayerData, int> changeMoney,
        Func<int, PlayerTokenMover> getPlayerMover,
        Action<PlayerData, BoardGridView> onPostMoveResolve)
    {
        if (view == null || currentPlayer == null)
        {
            return;
        }

        string tag = view.gameObject.tag;

        switch (tag)
        {
            case "Attack":
                ResolveAttack(view, currentPlayer, allPlayers, boardRegistry, logMessage, changeMoney);
                break;
            case "Buff":
                ResolveBuff(view, currentPlayer, allPlayers, boardRegistry, logMessage, changeMoney, getPlayerMover, onPostMoveResolve);
                break;
            case "Debuff":
                ResolveDebuff(view, currentPlayer, allPlayers, boardRegistry, logMessage, changeMoney);
                break;
            default:
                // logMessage(currentPlayer.playerName + " triggered an unknown event on grid " + view.GridIndex);
                logMessage(currentPlayer.playerName + " triggered an unknown event on grid " + view.GridIndex);
                break;
        }
    }

    // ==================== 攻击事件 / Attack Events ====================

    private static void ResolveAttack(
        BoardGridView view,
        PlayerData currentPlayer,
        List<PlayerData> allPlayers,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage,
        Action<PlayerData, int> changeMoney)
    {
        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                BananaPeel(view, currentPlayer, allPlayers, boardRegistry, logMessage);
                break;
            case 1:
                TempDinner(view, currentPlayer, allPlayers, changeMoney, logMessage);
                break;
            case 2:
                IndustryMerger(view, currentPlayer, allPlayers, boardRegistry, changeMoney, logMessage);
                break;
        }
    }

    /// <summary>香蕉皮：让一个对手后退1格。 / Banana peel: push one opponent back 1 grid.</summary>
    private static void BananaPeel(
        BoardGridView view,
        PlayerData currentPlayer,
        List<PlayerData> allPlayers,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage)
    {
        PlayerData target = PickOneOpponent(currentPlayer, allPlayers);
        if (target == null)
        {
            // logMessage(currentPlayer.playerName + " 触发香蕉皮，但没有可用的对手。");
            logMessage(currentPlayer.playerName + " triggered Banana Peel, but no opponent available.");
            return;
        }

        int boardSize = boardRegistry != null && boardRegistry.Count > 0 ? boardRegistry.Count : 36;
        target.position = (target.position - 1 + boardSize) % boardSize;
        // logMessage(currentPlayer.playerName + " 踩到香蕉皮让 " + target.playerName + " 后退1格，现在位于格子 " + target.position);
        logMessage(currentPlayer.playerName + " used Banana Peel! " + target.playerName + " moves back 1 grid (now at " + target.position + ").");
    }

    /// <summary>临时聚餐：对手 -5 金币，自己 +5 金币。 / Temp dinner: steal 5 gold from opponent.</summary>
    private static void TempDinner(
        BoardGridView view,
        PlayerData currentPlayer,
        List<PlayerData> allPlayers,
        Action<PlayerData, int> changeMoney,
        Action<string> logMessage)
    {
        PlayerData target = PickOneOpponent(currentPlayer, allPlayers);
        if (target == null)
        {
            // logMessage(currentPlayer.playerName + " 触发临时聚餐，但没有可用的对手。");
            logMessage(currentPlayer.playerName + " triggered Temp Dinner, but no opponent available.");
            return;
        }

        int stealAmount = 5;
        int actualStolen = Mathf.Min(target.money, stealAmount);
        if (actualStolen <= 0)
        {
            // logMessage(currentPlayer.playerName + " 与 " + target.playerName + " 临时聚餐，但对方没钱可拿。");
            logMessage(currentPlayer.playerName + " had dinner with " + target.playerName + ", but they have no money.");
            return;
        }

        changeMoney(target, -actualStolen);
        changeMoney(currentPlayer, actualStolen);
        // logMessage(currentPlayer.playerName + " 与 " + target.playerName + " 临时聚餐，获得 " + actualStolen + " 金币。");
        logMessage(currentPlayer.playerName + " had dinner with " + target.playerName + " and got " + actualStolen + " gold.");
    }

    /// <summary>产业兼并：夺取对手一个1级产业；若没有，改为夺取10金币。 / Industry merger: steal level-1 building, or 10 gold.</summary>
    private static void IndustryMerger(
        BoardGridView view,
        PlayerData currentPlayer,
        List<PlayerData> allPlayers,
        BoardGridRegistry boardRegistry,
        Action<PlayerData, int> changeMoney,
        Action<string> logMessage)
    {
        PlayerData target = PickOneOpponent(currentPlayer, allPlayers);
        if (target == null)
        {
            // logMessage(currentPlayer.playerName + " 触发产业兼并，但没有可用的对手。");
            logMessage(currentPlayer.playerName + " triggered Industry Merger, but no opponent available.");
            return;
        }

        // 找一个对手的1级建筑 / Find a level-1 building of the opponent
        BoardGridView level1Grid = FindOpponentLevel1Building(target, boardRegistry);
        if (level1Grid != null)
        {
            // 夺取建筑 / Take over the building
            GridData grid = level1Grid.RuntimeData;
            int oldOwnerId = grid.ownerPlayerId;

            // 从原主人名下移除 / Remove from original owner
            target.ownedGridIndexes.Remove(level1Grid.GridIndex);

            // 转移给当前玩家 / Transfer to current player
            level1Grid.SetBuildingOwner(currentPlayer, grid.buildingData.buildingType);
            level1Grid.RuntimeData.buildingData.level = grid.buildingData.level; // 保留等级（虽然这里固定为1级） / Keep level (always 1 here)

            if (!currentPlayer.ownedGridIndexes.Contains(level1Grid.GridIndex))
            {
                currentPlayer.ownedGridIndexes.Add(level1Grid.GridIndex);
            }

            // logMessage(currentPlayer.playerName + " 兼并了 " + target.playerName + " 的一个1级产业（格子 " + level1Grid.GridIndex + "）！");
            logMessage(currentPlayer.playerName + " merged " + target.playerName + "'s level-1 building at grid " + level1Grid.GridIndex + "!");
        }
        else
        {
            // 没有1级产业，夺取10金币 / No level-1 building, steal 10 gold
            int stealAmount = 10;
            int actualStolen = Mathf.Min(target.money, stealAmount);
            if (actualStolen <= 0)
            {
                // logMessage(currentPlayer.playerName + " 触发产业兼并，但 " + target.playerName + " 既无1级产业也无金币。");
                logMessage(currentPlayer.playerName + " triggered Industry Merger, but " + target.playerName + " has no building or gold.");
                return;
            }

            changeMoney(target, -actualStolen);
            changeMoney(currentPlayer, actualStolen);
            // logMessage(currentPlayer.playerName + " 兼并未果，从 " + target.playerName + " 夺取 " + actualStolen + " 金币。");
            logMessage(currentPlayer.playerName + " merger failed, stole " + actualStolen + " gold from " + target.playerName + " instead.");
        }
    }

    // ==================== 增益事件 / Buff Events ====================

    private static void ResolveBuff(
        BoardGridView view,
        PlayerData currentPlayer,
        List<PlayerData> allPlayers,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage,
        Action<PlayerData, int> changeMoney,
        Func<int, PlayerTokenMover> getPlayerMover,
        Action<PlayerData, BoardGridView> onPostMoveResolve)
    {
        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                FreeRide(view, currentPlayer, boardRegistry, logMessage, getPlayerMover, onPostMoveResolve);
                break;
            case 1:
                AngelFund(view, currentPlayer, changeMoney, logMessage);
                break;
            case 2:
                GoodManagement(view, currentPlayer, boardRegistry, logMessage);
                break;
        }
    }

    /// <summary>顺风车：往前走2格并结算新格子的建筑效果（不触发事件以避免递归）。 / Free ride: move forward 2 grids.</summary>
    private static void FreeRide(
        BoardGridView view,
        PlayerData currentPlayer,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage,
        Func<int, PlayerTokenMover> getPlayerMover,
        Action<PlayerData, BoardGridView> onPostMoveResolve)
    {
        int boardSize = boardRegistry != null && boardRegistry.Count > 0 ? boardRegistry.Count : 36;
        int oldPosition = currentPlayer.position;
        currentPlayer.position = (currentPlayer.position + 2) % boardSize;

        // logMessage(currentPlayer.playerName + " 搭了顺风车，从格子 " + oldPosition + " 走到格子 " + currentPlayer.position);
        logMessage(currentPlayer.playerName + " got a free ride from grid " + oldPosition + " to grid " + currentPlayer.position + ".");

        // 移动角色（如果有 mover） / Move token if mover exists
        PlayerTokenMover mover = getPlayerMover?.Invoke(currentPlayer.playerId);
        BoardGridView newGrid = boardRegistry?.GetView(currentPlayer.position);
        if (mover != null && newGrid != null)
        {
            mover.SnapToGrid(newGrid.transform);
        }

        // 结算新格子的建筑效果（过路费等），但不触发新事件 / Resolve new grid (pass fee only, no event)
        if (newGrid != null)
        {
            onPostMoveResolve?.Invoke(currentPlayer, newGrid);
        }
    }

    /// <summary>天使资金：获得10金币。 / Angel fund: gain 10 gold.</summary>
    private static void AngelFund(
        BoardGridView view,
        PlayerData currentPlayer,
        Action<PlayerData, int> changeMoney,
        Action<string> logMessage)
    {
        changeMoney(currentPlayer, 10);
        // logMessage(currentPlayer.playerName + " 获得天使投资，获得10金币。");
        logMessage(currentPlayer.playerName + " received angel funding: +10 gold.");
    }

    /// <summary>经营得当：随机一个建筑升1级（不会3升4）。 / Good management: upgrade one random building by 1.</summary>
    private static void GoodManagement(
        BoardGridView view,
        PlayerData currentPlayer,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage)
    {
        BoardGridView targetGrid = PickRandomOwnedUpgradableBuilding(currentPlayer, boardRegistry);
        if (targetGrid == null)
        {
            // logMessage(currentPlayer.playerName + " 经营得当，但没有可升级的建筑。");
            logMessage(currentPlayer.playerName + " managed well, but has no upgradable building.");
            return;
        }

        int oldLevel = targetGrid.RuntimeData.buildingData.level;
        targetGrid.UpgradeBuilding();
        int newLevel = targetGrid.RuntimeData.buildingData.level;
        // logMessage(currentPlayer.playerName + " 的产业（格子 " + targetGrid.GridIndex + "）经营得当，从 " + oldLevel + " 级升到 " + newLevel + " 级。");
        logMessage(currentPlayer.playerName + "'s building at grid " + targetGrid.GridIndex + " upgraded from Lv." + oldLevel + " to Lv." + newLevel + ".");
    }

    // ==================== 倒霉事件 / Debuff Events ====================

    private static void ResolveDebuff(
        BoardGridView view,
        PlayerData currentPlayer,
        List<PlayerData> allPlayers,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage,
        Action<PlayerData, int> changeMoney)
    {
        int roll = Random.Range(0, 3);

        switch (roll)
        {
            case 0:
                Thief(view, currentPlayer, changeMoney, logMessage);
                break;
            case 1:
                EconomicCrisis(view, currentPlayer, boardRegistry, logMessage);
                break;
            case 2:
                MeteorStrike(view, currentPlayer, boardRegistry, logMessage);
                break;
        }
    }

    /// <summary>小偷：失去5金币。 / Thief: lose 5 gold.</summary>
    private static void Thief(
        BoardGridView view,
        PlayerData currentPlayer,
        Action<PlayerData, int> changeMoney,
        Action<string> logMessage)
    {
        int loseAmount = Mathf.Min(currentPlayer.money, 5);
        if (loseAmount <= 0)
        {
            // logMessage(currentPlayer.playerName + " 遭遇小偷，但身无分文。");
            logMessage(currentPlayer.playerName + " encountered a thief, but has no money.");
            return;
        }

        changeMoney(currentPlayer, -loseAmount);
        // logMessage(currentPlayer.playerName + " 遭遇小偷，被偷去 " + loseAmount + " 金币。");
        logMessage(currentPlayer.playerName + " was robbed by a thief: -" + loseAmount + " gold.");
    }

    /// <summary>经济危机：随机一个建筑降1级（不会1降到0，即不拆毁）。 / Economic crisis: downgrade one random building by 1.</summary>
    private static void EconomicCrisis(
        BoardGridView view,
        PlayerData currentPlayer,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage)
    {
        BoardGridView targetGrid = PickRandomOwnedDowngradableBuilding(currentPlayer, boardRegistry);
        if (targetGrid == null)
        {
            // logMessage(currentPlayer.playerName + " 遭遇经济危机，但没有可降级的建筑。");
            logMessage(currentPlayer.playerName + " hit by economic crisis, but has no downgradable building.");
            return;
        }

        int oldLevel = targetGrid.RuntimeData.buildingData.level;
        targetGrid.RuntimeData.buildingData.level--;
        int newLevel = targetGrid.RuntimeData.buildingData.level;
        // logMessage(currentPlayer.playerName + " 的产业（格子 " + targetGrid.GridIndex + "）遭遇经济危机，从 " + oldLevel + " 级降到 " + newLevel + " 级。");
        logMessage(currentPlayer.playerName + "'s building at grid " + targetGrid.GridIndex + " downgraded from Lv." + oldLevel + " to Lv." + newLevel + ".");
    }

    /// <summary>陨石打击：随机一个建筑被拆毁。 / Meteor strike: destroy one random building.</summary>
    private static void MeteorStrike(
        BoardGridView view,
        PlayerData currentPlayer,
        BoardGridRegistry boardRegistry,
        Action<string> logMessage)
    {
        BoardGridView targetGrid = PickRandomOwnedBuilding(currentPlayer, boardRegistry);
        if (targetGrid == null)
        {
            // logMessage(currentPlayer.playerName + " 有陨石落下，但你没有建筑被砸中。");
            logMessage(currentPlayer.playerName + " saw a meteor fall, but no building was hit.");
            return;
        }

        int gridIndex = targetGrid.GridIndex;
        currentPlayer.ownedGridIndexes.Remove(gridIndex);
        targetGrid.ResetNeutralState();
        // logMessage(currentPlayer.playerName + " 的产业（格子 " + gridIndex + "）被陨石砸毁了！");
        logMessage(currentPlayer.playerName + "'s building at grid " + gridIndex + " was destroyed by a meteor!");
    }

    // ==================== 辅助方法 / Helper Methods ====================

    /// <summary>
    /// 从对手列表中随机选一个未破产的对手。
    /// Pick one non-bankrupt opponent at random.
    /// </summary>
    private static PlayerData PickOneOpponent(PlayerData currentPlayer, List<PlayerData> allPlayers)
    {
        if (allPlayers == null)
        {
            return null;
        }

        List<PlayerData> opponents = new List<PlayerData>();
        for (int i = 0; i < allPlayers.Count; i++)
        {
            PlayerData p = allPlayers[i];
            if (p == null || p.playerId == currentPlayer.playerId || p.IsBankrupt)
            {
                continue;
            }

            opponents.Add(p);
        }

        if (opponents.Count == 0)
        {
            return null;
        }

        return opponents[Random.Range(0, opponents.Count)];
    }

    /// <summary>
    /// 从对手名下找1级建筑。
    /// Find a level-1 building owned by the opponent.
    /// </summary>
    private static BoardGridView FindOpponentLevel1Building(PlayerData opponent, BoardGridRegistry registry)
    {
        if (opponent == null || registry == null)
        {
            return null;
        }

        List<BoardGridView> candidates = new List<BoardGridView>();
        for (int i = 0; i < registry.Count; i++)
        {
            BoardGridView view = registry.GetView(i);
            if (view == null)
            {
                continue;
            }

            GridData grid = view.RuntimeData;
            if (grid.HasBuilding &&
                grid.ownerPlayerId == opponent.playerId &&
                grid.buildingData.level == 1)
            {
                candidates.Add(view);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 从自己名下随机选一个可升级的建筑（1级或2级，未满级）。
    /// Pick a random upgradable building (level 1 or 2, not max).
    /// </summary>
    private static BoardGridView PickRandomOwnedUpgradableBuilding(PlayerData player, BoardGridRegistry registry)
    {
        if (player == null || registry == null)
        {
            return null;
        }

        List<BoardGridView> candidates = new List<BoardGridView>();
        for (int i = 0; i < registry.Count; i++)
        {
            BoardGridView view = registry.GetView(i);
            if (view == null)
            {
                continue;
            }

            GridData grid = view.RuntimeData;
            if (grid.HasBuilding &&
                grid.ownerPlayerId == player.playerId &&
                !grid.buildingData.IsMaxLevel)
            {
                candidates.Add(view);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 从自己名下随机选一个可降级的建筑（2级或3级，不会1降到0）。
    /// Pick a random downgradable building (level 2+, won't drop below 1).
    /// </summary>
    private static BoardGridView PickRandomOwnedDowngradableBuilding(PlayerData player, BoardGridRegistry registry)
    {
        if (player == null || registry == null)
        {
            return null;
        }

        List<BoardGridView> candidates = new List<BoardGridView>();
        for (int i = 0; i < registry.Count; i++)
        {
            BoardGridView view = registry.GetView(i);
            if (view == null)
            {
                continue;
            }

            GridData grid = view.RuntimeData;
            if (grid.HasBuilding &&
                grid.ownerPlayerId == player.playerId &&
                grid.buildingData.level >= 2) // 只有2级及以上才能降级 / Only level 2+ can be downgraded
            {
                candidates.Add(view);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 从自己名下随机选任意一个建筑（用于拆毁）。
    /// Pick a random owned building (for destruction).
    /// </summary>
    private static BoardGridView PickRandomOwnedBuilding(PlayerData player, BoardGridRegistry registry)
    {
        if (player == null || registry == null)
        {
            return null;
        }

        List<BoardGridView> candidates = new List<BoardGridView>();
        for (int i = 0; i < registry.Count; i++)
        {
            BoardGridView view = registry.GetView(i);
            if (view == null)
            {
                continue;
            }

            GridData grid = view.RuntimeData;
            if (grid.HasBuilding && grid.ownerPlayerId == player.playerId)
            {
                candidates.Add(view);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }
}
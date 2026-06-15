using UnityEngine;

[DisallowMultipleComponent]
public class BoardGridView : MonoBehaviour
{
    [SerializeField] private int gridIndex;
    [SerializeField] private GridData runtimeData = new GridData(); //格子数据

    public int GridIndex => gridIndex;
    public GridData RuntimeData => runtimeData;
    public bool IsBuildingGrid => gameObject.layer == LayerMask.NameToLayer("Building");
    public bool IsEventGrid => gameObject.layer == LayerMask.NameToLayer("Events");

    public void SyncFromScene() //同步数据，索引和类型
    {
        runtimeData.index = gridIndex;
        runtimeData.kind = IsEventGrid ? GridKind.Event : GridKind.Building;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncFromScene();
    }
#endif

    public void ResetNeutralState() //重置为无人状态
    {
        runtimeData.ownerPlayerId = -1;
        runtimeData.buildingData = null;

        if (IsBuildingGrid)
        {
            gameObject.tag = "Untagged";
        }
    }

    //设置建筑物归属和类型
    public void SetBuildingOwner(PlayerData owner, BuildingType buildingType)
    {
        runtimeData.ownerPlayerId = owner.playerId;
        runtimeData.buildingData = new BuildingData
        {
            buildingType = buildingType, 
            level = 1
        };

        gameObject.tag = owner.playerKind == PlayerKind.Player ? "Player" : "Enemy";
    }

    //升级建筑
    public void UpgradeBuilding()
    {
        //如果没有建筑或已满级则不能升级
        if (runtimeData.buildingData == null || runtimeData.buildingData.IsMaxLevel)
        {
            return;
        }

        runtimeData.buildingData.level++;
    }
}
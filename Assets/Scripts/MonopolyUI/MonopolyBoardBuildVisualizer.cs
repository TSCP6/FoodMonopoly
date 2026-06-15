using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MonopolyBoardBuildVisualizer : MonoBehaviour
{
    [SerializeField] private TurnStateMachine stateMachine;
    [SerializeField] private BoardGridRegistry boardRegistry;
    [SerializeField] private MonopolyHUD hud;

    [Header("Display")]
    [SerializeField] private bool showEmptyBuildingGrids = true;
    [SerializeField] private bool showEmptyBuildingLabels = false;
    [SerializeField] private bool showEventGrids = true;
    [SerializeField] private float markerHeight = 0.18f;
    [SerializeField] private float labelHeight = 1.1f;
    [SerializeField] private float labelCharacterSize = 0.045f;
    [SerializeField] private Vector3 labelBackScale = new Vector3(1.25f, 0.42f, 0.02f);
    [SerializeField] private float refreshInterval = 0.15f;

    private readonly List<GridVisual> visuals = new List<GridVisual>();
    private readonly Dictionary<BoardGridView, GridVisual> visualMap = new Dictionary<BoardGridView, GridVisual>();
    private readonly List<Material> runtimeMaterials = new List<Material>();

    private Material buildableMaterial;
    private Material playerMaterial;
    private Material enemyMaterial;
    private Material emptyMaterial;
    private Material eventMaterial;
    private Material textBackMaterial;

    private float nextRefreshTime;
    private Camera mainCamera;

    private void Awake()
    {
        ResolveReferences();
        CreateMaterials();
        RebuildVisuals();
    }

    private void OnEnable()
    {
        RefreshNow();
    }

    private void OnDestroy()
    {
        ClearVisuals();

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            if (runtimeMaterials[i] != null)
            {
                Destroy(runtimeMaterials[i]);
            }
        }

        runtimeMaterials.Clear();
    }

    private void Update()
    {
        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + refreshInterval;
        RefreshNow();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Quaternion labelRotation = mainCamera.transform.rotation;
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null && visuals[i].labelRoot != null)
            {
                visuals[i].labelRoot.transform.rotation = labelRotation;
            }
        }
    }

    public void Bind(TurnStateMachine targetStateMachine, BoardGridRegistry targetBoardRegistry, MonopolyHUD targetHud)
    {
        stateMachine = targetStateMachine;
        boardRegistry = targetBoardRegistry;
        hud = targetHud;
        RebuildVisuals();
        RefreshNow();
    }

    public void RefreshNow()
    {
        ResolveReferences();

        if (boardRegistry == null)
        {
            return;
        }

        if (visualMap.Count != boardRegistry.Count)
        {
            RebuildVisuals();
        }

        PlayerData currentPlayer = GetCurrentPlayer();
        BoardGridView currentGrid = GetCurrentGrid(currentPlayer);
        bool isOptionalAction = stateMachine != null && stateMachine.CurrentState == TurnState.OptionalAction;

        for (int i = 0; i < boardRegistry.Count; i++)
        {
            BoardGridView gridView = boardRegistry.GetView(i);
            if (gridView == null)
            {
                continue;
            }

            if (!visualMap.TryGetValue(gridView, out GridVisual visual))
            {
                continue;
            }

            RefreshGridVisual(visual, gridView, currentPlayer, currentGrid, isOptionalAction);
        }
    }

    private void ResolveReferences()
    {
        if (hud == null)
        {
            hud = FindObjectOfType<MonopolyHUD>();
        }

        if (stateMachine == null)
        {
            stateMachine = hud != null ? hud.BoundStateMachine : FindObjectOfType<TurnStateMachine>();
        }

        if (boardRegistry == null)
        {
            boardRegistry = hud != null ? hud.BoundBoardRegistry : FindObjectOfType<BoardGridRegistry>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void RebuildVisuals()
    {
        ClearVisuals();

        if (boardRegistry == null)
        {
            ResolveReferences();
        }

        if (boardRegistry == null)
        {
            return;
        }

        for (int i = 0; i < boardRegistry.Count; i++)
        {
            BoardGridView gridView = boardRegistry.GetView(i);
            if (gridView == null || visualMap.ContainsKey(gridView))
            {
                continue;
            }

            GridVisual visual = CreateGridVisual(gridView);
            visuals.Add(visual);
            visualMap.Add(gridView, visual);
        }
    }

    private GridVisual CreateGridVisual(BoardGridView gridView)
    {
        GameObject root = new GameObject("UI Grid Visual " + gridView.GridIndex);
        root.transform.SetParent(transform, false);
        root.transform.position = gridView.transform.position;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Marker";
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0f, markerHeight, 0f);
        marker.transform.localScale = new Vector3(0.72f, 0.025f, 0.72f);
        Destroy(marker.GetComponent<Collider>());

        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "Building Marker";
        building.transform.SetParent(root.transform, false);
        building.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        Destroy(building.GetComponent<Collider>());

        GameObject labelRoot = new GameObject("Label");
        labelRoot.transform.SetParent(root.transform, false);
        labelRoot.transform.localPosition = new Vector3(0f, labelHeight, 0f);

        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Label Back";
        back.transform.SetParent(labelRoot.transform, false);
        back.transform.localPosition = new Vector3(0f, 0f, 0.012f);
        back.transform.localScale = labelBackScale;
        Destroy(back.GetComponent<Collider>());
        back.GetComponent<Renderer>().sharedMaterial = textBackMaterial;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(labelRoot.transform, false);
        textObject.transform.localPosition = Vector3.zero;
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = labelCharacterSize;
        textMesh.fontSize = 48;
        textMesh.color = Color.black;

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            textMesh.font = font;
            MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sharedMaterial = font.material;
            }
        }

        return new GridVisual
        {
            root = root,
            markerRenderer = marker.GetComponent<Renderer>(),
            building = building,
            buildingRenderer = building.GetComponent<Renderer>(),
            labelRoot = labelRoot,
            labelBack = back,
            label = textMesh
        };
    }

    private void RefreshGridVisual(GridVisual visual, BoardGridView gridView, PlayerData currentPlayer, BoardGridView currentGrid, bool isOptionalAction)
    {
        GridData grid = gridView.RuntimeData;
        if (grid == null)
        {
            visual.SetVisible(false);
            return;
        }

        visual.root.transform.position = gridView.transform.position;

        if (gridView.IsEventGrid || grid.kind == GridKind.Event)
        {
            visual.SetVisible(showEventGrids);
            visual.SetBuildingVisible(false);
            visual.SetLabelVisible(showEventGrids);
            visual.markerRenderer.sharedMaterial = eventMaterial;
            visual.label.text = "事件\n不可建";
            return;
        }

        if (!gridView.IsBuildingGrid && grid.kind != GridKind.Building)
        {
            visual.SetVisible(false);
            return;
        }

        bool isHumanTurn = currentPlayer != null && currentPlayer.playerKind == PlayerKind.Player;
        bool isCurrentGrid = currentGrid == gridView;
        bool canBuildHere = isHumanTurn
            && isOptionalAction
            && isCurrentGrid
            && !grid.HasBuilding
            && CanAffordAnyBuild(currentPlayer);

        if (!grid.HasBuilding)
        {
            visual.SetVisible(showEmptyBuildingGrids || canBuildHere);
            visual.SetBuildingVisible(false);
            visual.SetLabelVisible(canBuildHere || showEmptyBuildingLabels);
            visual.markerRenderer.sharedMaterial = canBuildHere ? buildableMaterial : emptyMaterial;
            visual.label.text = canBuildHere
                ? "可建\n1:12  2:20\n3:18  0跳过"
                : "建筑格\n不可建";
            return;
        }

        PlayerData owner = GetOwner(grid.ownerPlayerId);
        bool ownedByHuman = owner == null
            ? currentPlayer != null && grid.ownerPlayerId == currentPlayer.playerId && currentPlayer.playerKind == PlayerKind.Player
            : owner.playerKind == PlayerKind.Player;

        visual.SetVisible(true);
        visual.SetBuildingVisible(true);
        visual.SetLabelVisible(true);
        visual.markerRenderer.sharedMaterial = ownedByHuman ? playerMaterial : enemyMaterial;
        visual.buildingRenderer.sharedMaterial = ownedByHuman ? playerMaterial : enemyMaterial;
        ApplyBuildingShape(visual, grid.buildingData);

        int income = GridRules.GetTurnIncome(grid.buildingData);
        int passFee = GridRules.GetPassFee(grid.buildingData);
        int upgradeCost = grid.buildingData.IsMaxLevel ? 0 : GridRules.GetUpgradeCost(grid.buildingData.buildingType, grid.buildingData.level);

        string ownerText = ownedByHuman ? "玩家" : "敌人";
        string upgradeText = grid.buildingData.IsMaxLevel ? "满" : "升" + upgradeCost;
        visual.label.text = ownerText + " " + ShortBuildingName(grid.buildingData.buildingType) + " L" + grid.buildingData.level
            + "\n" + upgradeText + " 收" + income + " 路" + passFee;
    }

    private void ApplyBuildingShape(GridVisual visual, BuildingData buildingData)
    {
        if (buildingData == null)
        {
            visual.SetBuildingVisible(false);
            return;
        }

        float height = 0.38f + buildingData.level * 0.18f;
        float width = 0.28f;

        switch (buildingData.buildingType)
        {
            case BuildingType.CrownRestaurant:
                width = 0.38f;
                break;
            case BuildingType.FineRestaurant:
                width = 0.24f;
                height += 0.12f;
                break;
        }

        visual.building.transform.localScale = new Vector3(width, height, width);
        visual.building.transform.localPosition = new Vector3(0f, markerHeight + height * 0.5f + 0.05f, 0f);
    }

    private PlayerData GetCurrentPlayer()
    {
        if (hud != null && hud.CurrentPlayer != null)
        {
            return hud.CurrentPlayer;
        }

        if (stateMachine == null)
        {
            return null;
        }

        List<PlayerData> players = stateMachine.GetAllPlayers();
        if (players == null || players.Count == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(stateMachine.CurrentPlayerIndex, 0, players.Count - 1);
        return players[index];
    }

    private PlayerData GetOwner(int ownerPlayerId)
    {
        if (ownerPlayerId < 0 || stateMachine == null)
        {
            return null;
        }

        return stateMachine.GetPlayer(ownerPlayerId);
    }

    private BoardGridView GetCurrentGrid(PlayerData player)
    {
        if (hud != null && hud.LastOptionalContext != null && hud.LastOptionalContext.currentGridView != null)
        {
            return hud.LastOptionalContext.currentGridView;
        }

        if (player == null || boardRegistry == null)
        {
            return null;
        }

        return boardRegistry.GetView(player.position);
    }

    private bool CanAffordAnyBuild(PlayerData player)
    {
        return player.money >= GridRules.GetBuildCost(BuildingType.ChainRestaurant)
            || player.money >= GridRules.GetBuildCost(BuildingType.CrownRestaurant)
            || player.money >= GridRules.GetBuildCost(BuildingType.FineRestaurant);
    }

    private string ShortBuildingName(BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.CrownRestaurant:
                return "皇冠";
            case BuildingType.FineRestaurant:
                return "精致";
            default:
                return "连锁";
        }
    }

    private void CreateMaterials()
    {
        buildableMaterial = CreateTransparentMaterial("Buildable Grid", new Color(0.2f, 1f, 0.35f, 0.58f));
        playerMaterial = CreateTransparentMaterial("Player Building", new Color(0.25f, 0.55f, 1f, 0.78f));
        enemyMaterial = CreateTransparentMaterial("Enemy Building", new Color(1f, 0.25f, 0.2f, 0.78f));
        emptyMaterial = CreateTransparentMaterial("Empty Building Grid", new Color(0.82f, 0.82f, 0.82f, 0.32f));
        eventMaterial = CreateTransparentMaterial("Event Grid", new Color(1f, 0.85f, 0.2f, 0.42f));
        textBackMaterial = CreateTransparentMaterial("Label Back", new Color(1f, 1f, 1f, 0.72f));
    }

    private Material CreateTransparentMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        runtimeMaterials.Add(material);
        return material;
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null && visuals[i].root != null)
            {
                Destroy(visuals[i].root);
            }
        }

        visuals.Clear();
        visualMap.Clear();
    }

    private class GridVisual
    {
        public GameObject root;
        public Renderer markerRenderer;
        public GameObject building;
        public Renderer buildingRenderer;
        public GameObject labelRoot;
        public GameObject labelBack;
        public TextMesh label;

        public void SetVisible(bool visible)
        {
            if (root != null && root.activeSelf != visible)
            {
                root.SetActive(visible);
            }
        }

        public void SetBuildingVisible(bool visible)
        {
            if (building != null && building.activeSelf != visible)
            {
                building.SetActive(visible);
            }
        }

        public void SetLabelVisible(bool visible)
        {
            if (labelRoot != null && labelRoot.activeSelf != visible)
            {
                labelRoot.SetActive(visible);
            }
        }
    }
}

using System;
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
    [SerializeField] private bool showGridInfoLabels = true;
    [SerializeField] private bool showEmptyBuildingLabels = false;
    [SerializeField] private bool showEventGrids = true;
    [SerializeField] private KeyCode toggleGridInfoLabelsKey = KeyCode.I;
    [SerializeField] private float markerHeight = 0.18f;
    [SerializeField] private float labelHeight = 1.1f;
    [SerializeField] private float labelCharacterSize = 0.035f;
    [SerializeField] private Vector3 labelBackScale = new Vector3(1.25f, 0.42f, 0.02f);
    [SerializeField] private float buildingSideOffset = 1.1f;
    [SerializeField] private bool snapBuildingOffsetToFourDirections = true;
    [SerializeField] private float refreshInterval = 0.15f;

    [Header("Building Prefabs")]
    [SerializeField] private List<BuildingPrefabEntry> buildingPrefabs = new List<BuildingPrefabEntry>();

    private readonly List<GridVisual> visuals = new List<GridVisual>();
    private readonly Dictionary<BoardGridView, GridVisual> visualMap = new Dictionary<BoardGridView, GridVisual>();
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly Dictionary<(BuildingType, int), GameObject> prefabLookup = new Dictionary<(BuildingType, int), GameObject>();

    private Material buildableMaterial;
    private Material playerMaterial;
    private Material enemyMaterial;
    private Material emptyMaterial;
    private Material eventMaterial;
    private Material hoverMaterial;
    private Material textBackMaterial;
    private Material selectedLabelBackMaterial;

    private float nextRefreshTime;
    private Camera mainCamera;
    private BoardGridView hoveredGrid;

    public BoardGridView HoveredGrid => hoveredGrid;

    [Serializable]
    public class BuildingPrefabEntry
    {
        public BuildingType buildingType;
        public int level = 1;
        public GameObject prefab;
    }

    private void Awake()
    {
        BuildPrefabLookup();
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
        prefabLookup.Clear();
    }

    private void Update()
    {
        if (toggleGridInfoLabelsKey != KeyCode.None && Input.GetKeyDown(toggleGridInfoLabelsKey))
        {
            SetGridInfoLabelsVisible(!showGridInfoLabels);
        }

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
        BuildPrefabLookup();
        RebuildVisuals();
        RefreshNow();
    }

    public void SetGridInfoLabelsVisible(bool visible)
    {
        showGridInfoLabels = visible;
        RefreshNow();
    }

    public void SetHoveredGrid(BoardGridView gridView)
    {
        if (hoveredGrid == gridView)
        {
            return;
        }

        hoveredGrid = gridView;
        RefreshNow();
    }

    /// <summary>
    /// 从模板复制建筑预制体列表。呼叫后自动重建 prefabLookup。
    /// 用于运行时动态创建 Visualizer 时注入 Inspector 中配置的模板数据。
    /// </summary>
    public void CopyPrefabsFromTemplate(MonopolyBoardBuildVisualizer template)
    {
        if (template == null)
        {
            return;
        }

        buildingPrefabs.Clear();
        for (int i = 0; i < template.buildingPrefabs.Count; i++)
        {
            buildingPrefabs.Add(template.buildingPrefabs[i]);
        }

        BuildPrefabLookup();
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

    private void BuildPrefabLookup()
    {
        prefabLookup.Clear();
        for (int i = 0; i < buildingPrefabs.Count; i++)
        {
            BuildingPrefabEntry entry = buildingPrefabs[i];
            if (entry.prefab == null)
            {
                continue;
            }

            var key = (entry.buildingType, entry.level);
            if (!prefabLookup.ContainsKey(key))
            {
                prefabLookup.Add(key, entry.prefab);
            }
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

        // 建筑容器：预制体实例将挂在这个容器下
        GameObject buildingContainer = new GameObject("Building Container");
        buildingContainer.transform.SetParent(root.transform, false);
        buildingContainer.transform.localPosition = GetBuildingLocalPosition(gridView);
        buildingContainer.transform.localRotation = Quaternion.identity;

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
            buildingContainer = buildingContainer,
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
        visual.buildingContainer.transform.localPosition = GetBuildingLocalPosition(gridView);
        bool isHovered = hoveredGrid == gridView;

        if (gridView.IsEventGrid || grid.kind == GridKind.Event)
        {
            visual.SetVisible(showEventGrids);
            visual.SetBuildingVisible(false);
            visual.SetLabelVisible(showGridInfoLabels && showEventGrids);
            visual.markerRenderer.sharedMaterial = isHovered ? hoverMaterial : eventMaterial;
            visual.SetLabelBackMaterial(isHovered ? selectedLabelBackMaterial : textBackMaterial);
            visual.ApplyHoverMaterial(false, hoverMaterial);
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
            visual.SetLabelVisible(showGridInfoLabels && (canBuildHere || showEmptyBuildingLabels));
            visual.markerRenderer.sharedMaterial = isHovered ? hoverMaterial : canBuildHere ? buildableMaterial : emptyMaterial;
            visual.SetLabelBackMaterial(isHovered ? selectedLabelBackMaterial : textBackMaterial);
            visual.ApplyHoverMaterial(false, hoverMaterial);
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
        visual.SetLabelVisible(showGridInfoLabels);
        visual.markerRenderer.sharedMaterial = isHovered ? hoverMaterial : ownedByHuman ? playerMaterial : enemyMaterial;
        visual.SetLabelBackMaterial(isHovered ? selectedLabelBackMaterial : textBackMaterial);
        ApplyBuildingPrefab(visual, gridView, grid.buildingData);
        visual.ApplyHoverMaterial(isHovered, hoverMaterial);

        string ownerText = ownedByHuman ? "\u73A9\u5BB6" : "\u654C\u4EBA";
        visual.label.text = ShortBuildingName(grid.buildingData.buildingType) + " L" + grid.buildingData.level
            + "\n" + ownerText;
    }

    private void ApplyBuildingPrefab(GridVisual visual, BoardGridView gridView, BuildingData buildingData)
    {
        if (buildingData == null)
        {
            visual.DestroyBuildingInstance();
            visual.SetBuildingVisible(false);
            return;
        }

        var key = (buildingData.buildingType, buildingData.level);

        // 如果已有实例且预制体没变，不需要重建
        if (visual.currentPrefabKey.HasValue && visual.currentPrefabKey.Value == key && visual.buildingInstance != null)
        {
            return;
        }

        // 销毁旧实例
        visual.DestroyBuildingInstance();

        // 查找对应预制体
        if (!prefabLookup.TryGetValue(key, out GameObject prefab))
        {
            Debug.LogWarning($"[MonopolyBoardBuildVisualizer] 未找到预制体: BuildingType={buildingData.buildingType}, Level={buildingData.level}");
            visual.SetBuildingVisible(false);
            return;
        }

        // 实例化预制体，挂到容器下
        GameObject instance = Instantiate(prefab, visual.buildingContainer.transform);
        instance.name = $"Building_{buildingData.buildingType}_Lv{buildingData.level}";
        instance.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        instance.SetActive(true);
        PrepareBuildingClickTarget(instance, gridView);

        // 收集所有 Renderer，保留 prefab 自带材质，避免覆盖原贴图和多材质槽。
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            visual.buildingRenderers = renderers;
            visual.CacheOriginalMaterials();
        }

        visual.buildingInstance = instance;
        visual.currentPrefabKey = key;
    }

    private Vector3 GetBuildingLocalPosition(BoardGridView gridView)
    {
        if (gridView == null || buildingSideOffset <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 boardCenter = GetBoardCenter();
        Vector3 outward = gridView.transform.position - boardCenter;
        outward.y = 0f;

        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = gridView.transform.forward;
            outward.y = 0f;
        }

        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = Vector3.forward;
        }

        if (snapBuildingOffsetToFourDirections)
        {
            outward = Mathf.Abs(outward.x) >= Mathf.Abs(outward.z)
                ? new Vector3(Mathf.Sign(outward.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(outward.z));
        }
        else
        {
            outward.Normalize();
        }

        return outward * buildingSideOffset;
    }

    private void PrepareBuildingClickTarget(GameObject instance, BoardGridView gridView)
    {
        if (instance == null)
        {
            return;
        }

        MonopolyBuildingClickTarget clickTarget = instance.GetComponent<MonopolyBuildingClickTarget>();
        if (clickTarget == null)
        {
            clickTarget = instance.AddComponent<MonopolyBuildingClickTarget>();
        }

        clickTarget.Bind(gridView);

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        bool hasEnabledCollider = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = true;
                hasEnabledCollider = true;
            }
        }

        if (!hasEnabledCollider)
        {
            Bounds bounds = CalculateRendererBounds(instance);
            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.center = instance.transform.InverseTransformPoint(bounds.center);
            collider.size = instance.transform.InverseTransformVector(bounds.size);
        }
    }

    private Bounds CalculateRendererBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(instance.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private Vector3 GetBoardCenter()
    {
        if (boardRegistry == null || boardRegistry.Count == 0)
        {
            return transform.position;
        }

        Vector3 sum = Vector3.zero;
        int validCount = 0;
        for (int i = 0; i < boardRegistry.Count; i++)
        {
            BoardGridView view = boardRegistry.GetView(i);
            if (view == null)
            {
                continue;
            }

            sum += view.transform.position;
            validCount++;
        }

        return validCount > 0 ? sum / validCount : transform.position;
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
        playerMaterial = CreateTransparentMaterial("Player Building Marker", new Color(0.25f, 0.55f, 1f, 0.78f));
        enemyMaterial = CreateTransparentMaterial("Enemy Building Marker", new Color(1f, 0.25f, 0.2f, 0.78f));
        emptyMaterial = CreateTransparentMaterial("Empty Building Grid", new Color(0.82f, 0.82f, 0.82f, 0.32f));
        eventMaterial = CreateTransparentMaterial("Event Grid", new Color(1f, 0.85f, 0.2f, 0.42f));
        hoverMaterial = CreateTransparentMaterial("Hovered Building", new Color(1f, 1f, 1f, 0.58f));
        textBackMaterial = CreateSimpleTransparentMaterial("Label Back", new Color(1f, 1f, 1f, 0.72f));
        selectedLabelBackMaterial = CreateSimpleTransparentMaterial("Selected Label Back", new Color(1f, 0.92f, 0.05f, 0.86f));
    }

    private Material CreateSimpleTransparentMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = materialName;
        ApplyMaterialColor(material, color);
        ConfigureTransparentMaterial(material);
        runtimeMaterials.Add(material);
        return material;
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
        ApplyMaterialColor(material, color);
        ConfigureTransparentMaterial(material);
        runtimeMaterials.Add(material);
        return material;
    }

    private void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        material.color = color;

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
    }

    private void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

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
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
            {
                visuals[i].DestroyBuildingInstance();

                if (visuals[i].root != null)
                {
                    Destroy(visuals[i].root);
                }
            }
        }

        visuals.Clear();
        visualMap.Clear();
    }

    private class GridVisual
    {
        public GameObject root;
        public Renderer markerRenderer;
        /// <summary>建筑预制体实例的父容器</summary>
        public GameObject buildingContainer;
        /// <summary>当前实例化的建筑预制体</summary>
        public GameObject buildingInstance;
        /// <summary>建筑预制体上的 Renderer 组件（用于着色）</summary>
        public Renderer[] buildingRenderers;
        public Material[][] originalMaterials;
        /// <summary>当前实例的预制体标识，用于判断是否需要切换</summary>
        public (BuildingType, int)? currentPrefabKey;
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
            if (buildingContainer != null && buildingContainer.activeSelf != visible)
            {
                buildingContainer.SetActive(visible);
            }
        }

        public void SetLabelVisible(bool visible)
        {
            if (labelRoot != null && labelRoot.activeSelf != visible)
            {
                labelRoot.SetActive(visible);
            }
        }

        public void SetLabelBackMaterial(Material material)
        {
            if (labelBack == null || material == null)
            {
                return;
            }

            Renderer renderer = labelBack.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
            }
        }

        public void CacheOriginalMaterials()
        {
            if (buildingRenderers == null)
            {
                originalMaterials = null;
                return;
            }

            originalMaterials = new Material[buildingRenderers.Length][];
            for (int i = 0; i < buildingRenderers.Length; i++)
            {
                originalMaterials[i] = buildingRenderers[i] == null ? null : buildingRenderers[i].sharedMaterials;
            }
        }

        public void ApplyHoverMaterial(bool hovered, Material hoverMaterial)
        {
            if (buildingRenderers == null)
            {
                return;
            }

            if (originalMaterials == null || originalMaterials.Length != buildingRenderers.Length)
            {
                CacheOriginalMaterials();
            }

            for (int i = 0; i < buildingRenderers.Length; i++)
            {
                Renderer renderer = buildingRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!hovered)
                {
                    if (originalMaterials != null && i < originalMaterials.Length && originalMaterials[i] != null)
                    {
                        renderer.sharedMaterials = originalMaterials[i];
                    }

                    continue;
                }

                Material[] currentMaterials = renderer.sharedMaterials;
                if (currentMaterials == null || currentMaterials.Length == 0)
                {
                    renderer.sharedMaterial = hoverMaterial;
                    continue;
                }

                Material[] hoverMaterials = new Material[currentMaterials.Length];
                for (int j = 0; j < hoverMaterials.Length; j++)
                {
                    hoverMaterials[j] = hoverMaterial;
                }

                renderer.sharedMaterials = hoverMaterials;
            }
        }

        public void DestroyBuildingInstance()
        {
            if (buildingInstance != null)
            {
                UnityEngine.Object.Destroy(buildingInstance);
                buildingInstance = null;
            }

            buildingRenderers = null;
            originalMaterials = null;
            currentPrefabKey = null;
        }
    }
}

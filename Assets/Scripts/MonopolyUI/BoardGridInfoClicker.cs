using UnityEngine;
using UnityEngine.EventSystems;

public class BoardGridInfoClicker : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MonopolyHUD hud;
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private float maxDistance = 1000f;
    [SerializeField] private bool ignoreClicksOverUI = true;
    [SerializeField] private float hoverConfirmDelay = 0.2f;

    private BoardGridView candidateHoverGrid;
    private BoardGridView hoveredGrid;
    private float candidateHoverStartTime;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    public void Bind(MonopolyHUD targetHud, Camera cameraOverride = null)
    {
        hud = targetHud;
        if (cameraOverride != null)
        {
            targetCamera = cameraOverride;
        }

        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (targetCamera == null || hud == null)
        {
            return;
        }

        bool pointerOverUI = ignoreClicksOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        BoardGridView gridView = null;
        if (!pointerOverUI)
        {
            TryGetGridUnderMouse(out gridView);
        }

        UpdateHoverTarget(gridView);

        if (!Input.GetMouseButtonDown(0) || pointerOverUI || gridView == null)
        {
            return;
        }

        hud.HandleBoardGridClicked(gridView);
    }

    private void UpdateHoverTarget(BoardGridView gridView)
    {
        if (candidateHoverGrid != gridView)
        {
            candidateHoverGrid = gridView;
            candidateHoverStartTime = Time.unscaledTime;

            if (gridView == null && hoveredGrid != null)
            {
                hoveredGrid = null;
                hud.HandleBoardGridHovered(null);
            }

            return;
        }

        if (hoveredGrid == gridView)
        {
            return;
        }

        if (gridView == null)
        {
            return;
        }

        if (Time.unscaledTime - candidateHoverStartTime < hoverConfirmDelay)
        {
            return;
        }

        hoveredGrid = gridView;
        hud.HandleBoardGridHovered(hoveredGrid);
    }

    private bool TryGetGridUnderMouse(out BoardGridView gridView)
    {
        gridView = null;

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, clickableLayers, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null)
            {
                continue;
            }

            MonopolyBuildingClickTarget buildingTarget = hits[i].collider.GetComponentInParent<MonopolyBuildingClickTarget>();
            if (buildingTarget != null && buildingTarget.GridView != null)
            {
                gridView = buildingTarget.GridView;
                return true;
            }

            BoardGridView candidate = hits[i].collider.GetComponentInParent<BoardGridView>();
            if (candidate != null)
            {
                gridView = candidate;
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (hud == null)
        {
            hud = FindObjectOfType<MonopolyHUD>();
        }
    }
}

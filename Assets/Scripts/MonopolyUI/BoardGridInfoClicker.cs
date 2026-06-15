using UnityEngine;
using UnityEngine.EventSystems;

public class BoardGridInfoClicker : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MonopolyHUD hud;
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private float maxDistance = 1000f;
    [SerializeField] private bool ignoreClicksOverUI = true;

    private void Awake()
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

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (ignoreClicksOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (targetCamera == null || hud == null)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableLayers, QueryTriggerInteraction.Collide))
        {
            return;
        }

        BoardGridView gridView = hit.collider.GetComponentInParent<BoardGridView>();
        if (gridView != null)
        {
            hud.HandleBoardGridClicked(gridView);
        }
    }
}

using UnityEngine;

[DisallowMultipleComponent]
public class MonopolyHUDTurnStateBridge : MonoBehaviour
{
    [SerializeField] private MonopolyHUD hud;
    [SerializeField] private TurnStateMachine turnStateMachine;
    [SerializeField] private BoardGridRegistry boardRegistry;
    [SerializeField] private bool bindOnAwake = true;

    private bool subscribed;

    private void Awake()
    {
        ResolveReferences();

        if (bindOnAwake)
        {
            BindHUD();
        }
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshButtonState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void BindHUD()
    {
        ResolveReferences();

        if (hud == null)
        {
            return;
        }

        if (turnStateMachine != null)
        {
            hud.BindTurnStateMachine(turnStateMachine);
        }

        if (boardRegistry != null)
        {
            hud.BindBoardRegistry(boardRegistry);
        }
    }

    public void RequestTurnStart()
    {
        if (turnStateMachine != null)
        {
            turnStateMachine.RequestTurnStart();
        }
    }

    private void ResolveReferences()
    {
        if (hud == null)
        {
            hud = FindSceneObjectIncludingInactive<MonopolyHUD>();
        }

        if (turnStateMachine == null)
        {
            turnStateMachine = FindSceneObjectIncludingInactive<TurnStateMachine>();
        }

        if (boardRegistry == null)
        {
            boardRegistry = FindSceneObjectIncludingInactive<BoardGridRegistry>();
        }
    }

    private T FindSceneObjectIncludingInactive<T>() where T : Component
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < objects.Length; i++)
        {
            T candidate = objects[i];
            if (candidate == null || candidate.gameObject == null)
            {
                continue;
            }

            if (candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        if (hud != null)
        {
            hud.OnActionClicked.AddListener(HandleHUDActionClicked);
        }

        if (turnStateMachine != null)
        {
            turnStateMachine.OnStateChanged += HandleStateChanged;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (hud != null)
        {
            hud.OnActionClicked.RemoveListener(HandleHUDActionClicked);
        }

        if (turnStateMachine != null)
        {
            turnStateMachine.OnStateChanged -= HandleStateChanged;
        }

        subscribed = false;
    }

    private void HandleHUDActionClicked(MonopolyUIActionType actionType)
    {
        if (actionType == MonopolyUIActionType.RollDiceOrNextTurn)
        {
            RequestTurnStart();
        }
    }

    private void HandleStateChanged(TurnState state)
    {
        if (hud == null)
        {
            return;
        }

        hud.SetDiceButtonInteractable(state == TurnState.TurnStart);
    }

    private void RefreshButtonState()
    {
        if (hud != null && turnStateMachine != null)
        {
            hud.SetDiceButtonInteractable(turnStateMachine.CurrentState == TurnState.TurnStart);
        }
    }
}

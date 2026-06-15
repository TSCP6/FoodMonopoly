using UnityEngine;
using UnityEngine.UI;

// 把 UI 按钮绑定到 TurnStateMachine 的“开始当前回合”接口。
// 按钮点击后，才真正进入掷骰和后续回合流程。
public class TurnStartButtonBinder : MonoBehaviour
{
    [SerializeField] private TurnStateMachine turnStateMachine;
    [SerializeField] private Button turnButton;

    private void Awake()
    {
        if (turnStateMachine == null)
        {
            turnStateMachine = FindObjectOfType<TurnStateMachine>();
        }
    }

    private void OnEnable()
    {
        if (turnButton != null)
        {
            turnButton.onClick.AddListener(HandleButtonClicked);
        }

        if (turnStateMachine != null)
        {
            turnStateMachine.OnStateChanged += HandleStateChanged;
            HandleStateChanged(turnStateMachine.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (turnButton != null)
        {
            turnButton.onClick.RemoveListener(HandleButtonClicked);
        }

        if (turnStateMachine != null)
        {
            turnStateMachine.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleButtonClicked()
    {
        if (turnStateMachine != null)
        {
            turnStateMachine.RequestTurnStart();
        }
    }

    private void HandleStateChanged(TurnState state)
    {
        if (turnButton != null)
        {
            turnButton.interactable = state == TurnState.TurnStart;
        }
    }
}
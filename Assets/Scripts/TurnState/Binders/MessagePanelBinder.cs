using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 信息栏绑定器。
// 把 TurnStateMachine 发出的消息显示到 UI 文本里。
public class MessagePanelBinder : MonoBehaviour
{
    [SerializeField] private TurnStateMachine turnStateMachine;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private int maxLines = 6;

    private readonly List<string> lines = new List<string>();

    private void Awake()
    {
        if (turnStateMachine == null)
        {
            turnStateMachine = FindObjectOfType<TurnStateMachine>();
        }
    }

    private void OnEnable()
    {
        if (turnStateMachine != null)
        {
            turnStateMachine.OnMessage += HandleMessage;
        }
    }

    private void OnDisable()
    {
        if (turnStateMachine != null)
        {
            turnStateMachine.OnMessage -= HandleMessage;
        }
    }

    private void HandleMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        lines.Add(message);
        while (lines.Count > maxLines)
        {
            lines.RemoveAt(0);
        }

        if (messageText != null)
        {
            messageText.text = string.Join("\n", lines.ToArray());
        }
    }
}
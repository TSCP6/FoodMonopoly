using System;
using System.Collections;
using UnityEngine;

// 独立骰子脚本。
// 当前只负责掷骰结果，不做骰子模型动画。
public class DiceRoller : MonoBehaviour
{
    [Header("Roll")]
    [SerializeField] private float rollDelay = 0.2f; //掷骰延迟，模拟思考/等待感

    // 掷骰协程。
    // 不做任何模型表现，只等待一个短时间后返回 1~6 的结果。
    public IEnumerator RollRoutine(Action<int> onCompleted)
    {
        yield return new WaitForSeconds(rollDelay);

        int finalValue = UnityEngine.Random.Range(1, 7);
        onCompleted?.Invoke(finalValue);
    }
}
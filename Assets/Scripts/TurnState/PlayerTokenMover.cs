using System;
using System.Collections;
using UnityEngine;

// 独立的角色移动脚本。
// 负责把角色移动到指定格子的 x/z 位置，y 锁定为 0.52，或者走一个简单跳跃轨迹。
public class PlayerTokenMover : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int playerId;

    [Header("Move")]
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private float fixedY = 0.52f;
    [SerializeField] private bool useJumpMove = false;
    [SerializeField] private float jumpHeight = 0.35f;

    public int PlayerId => playerId;

    // 直接把角色放到目标格子中心。
    public void SnapToGrid(Transform gridTransform)
    {
        if (gridTransform == null)
        {
            return;
        }

        Vector3 target = gridTransform.position;
        transform.position = new Vector3(target.x, fixedY, target.z);
    }

    // 平滑移动到目标格子。
    // useJumpMove 打开时，y 会沿抛物线抬起再落下。
    public IEnumerator MoveToGrid(Transform gridTransform, Action onCompleted = null)
    {
        if (gridTransform == null)
        {
            onCompleted?.Invoke();
            yield break;
        }

        Vector3 start = transform.position;
        Vector3 end = gridTransform.position;
        end.y = fixedY;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            Vector3 position = Vector3.Lerp(start, end, t);
            if (useJumpMove)
            {
                position.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            }
            else
            {
                position.y = fixedY;
            }

            transform.position = position;
            yield return null;
        }

        transform.position = end;
        onCompleted?.Invoke();
    }
}
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

    [Header("Facing")]
    [SerializeField] private bool faceMoveDirection = true;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float modelYawOffset;

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
        Quaternion targetRotation = GetMoveRotation(start, end);

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
            ApplyMoveRotation(targetRotation, Time.deltaTime);
            yield return null;
        }

        transform.position = end;
        ApplyMoveRotation(targetRotation, moveDuration);
        onCompleted?.Invoke();
    }

    private Quaternion GetMoveRotation(Vector3 start, Vector3 end)
    {
        Transform rotateTarget = GetRotateTarget();
        if (!faceMoveDirection || rotateTarget == null)
        {
            return Quaternion.identity;
        }

        Vector3 direction = end - start;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return rotateTarget.rotation;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    private void ApplyMoveRotation(Quaternion targetRotation, float deltaTime)
    {
        Transform rotateTarget = GetRotateTarget();
        if (!faceMoveDirection || rotateTarget == null || targetRotation == Quaternion.identity)
        {
            return;
        }

        if (turnSpeed <= 0f)
        {
            rotateTarget.rotation = targetRotation;
            return;
        }

        float t = Mathf.Clamp01(deltaTime * turnSpeed);
        rotateTarget.rotation = Quaternion.Slerp(rotateTarget.rotation, targetRotation, t);
    }

    private Transform GetRotateTarget()
    {
        return visualRoot == null ? transform : visualRoot;
    }
}

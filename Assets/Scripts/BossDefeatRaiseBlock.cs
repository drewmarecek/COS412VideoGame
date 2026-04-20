using System.Collections;
using UnityEngine;

/// <summary>
/// Raises this block when a boss is defeated.
/// Attach to each block that should rise to open the path forward.
/// </summary>
public class BossDefeatRaiseBlock : MonoBehaviour
{
    [Header("Boss Source (assign one or both)")]
    [SerializeField] private BossController boss1;
    [SerializeField] private boss2Script boss2;
    [SerializeField] private bool autoFindBossIfMissing = true;

    [Header("Block Motion")]
    [Tooltip("If enabled, the block starts below its placed position and rises to the placed position after boss defeat.")]
    [SerializeField] private bool startBelowGround = true;
    [Tooltip("How far below the placed position to start when Start Below Ground is enabled.")]
    [SerializeField] private float hiddenOffsetY = 2f;
    [Tooltip("How long the rise animation takes.")]
    [SerializeField] private float riseDuration = 1f;
    [SerializeField] private AnimationCurve riseEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float riseDelay = 0f;

    private Vector3 raisedPosition;
    private Vector3 loweredPosition;
    private bool hasStartedRising;
    private Coroutine riseRoutine;

    private void Awake()
    {
        if (autoFindBossIfMissing)
        {
            if (boss1 == null)
                boss1 = FindFirstObjectByType<BossController>();
            if (boss2 == null)
                boss2 = FindFirstObjectByType<boss2Script>();
        }

        raisedPosition = transform.position;
        loweredPosition = raisedPosition + Vector3.down * Mathf.Abs(hiddenOffsetY);

        if (startBelowGround)
            transform.position = loweredPosition;
    }

    private void Update()
    {
        if (hasStartedRising) return;
        if (!AnyAssignedBossDefeated()) return;

        hasStartedRising = true;
        riseRoutine = StartCoroutine(RaiseBlockRoutine());
    }

    private bool AnyAssignedBossDefeated()
    {
        bool hasAnyBossAssigned = boss1 != null || boss2 != null;
        if (!hasAnyBossAssigned) return false;

        if (boss1 != null && boss1.IsDefeated) return true;
        if (boss2 != null && boss2.IsDefeated) return true;
        return false;
    }

    private IEnumerator RaiseBlockRoutine()
    {
        if (riseDelay > 0f)
            yield return new WaitForSeconds(riseDelay);

        Vector3 from = transform.position;
        Vector3 to = raisedPosition;
        float duration = Mathf.Max(0.01f, riseDuration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = riseEase.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        transform.position = to;
        riseRoutine = null;
    }

    [ContextMenu("Raise Now")]
    private void RaiseNow()
    {
        if (riseRoutine != null)
            StopCoroutine(riseRoutine);
        hasStartedRising = true;
        transform.position = raisedPosition;
    }

    [ContextMenu("Lower Now")]
    private void LowerNow()
    {
        if (riseRoutine != null)
            StopCoroutine(riseRoutine);
        hasStartedRising = false;
        transform.position = loweredPosition;
    }
}

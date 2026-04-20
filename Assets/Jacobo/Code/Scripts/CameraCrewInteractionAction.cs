using UnityEngine;

public class CameraCrewInteractionAction : InteractionActionBase
{
    [Header("Cámara")]
    [SerializeField] private Transform cameraRig;
    [Tooltip("Opciones de cámara por índice de pose (0,1,2)")]
    [SerializeField] private Transform[] cameraOptionSpots = new Transform[3];
    [SerializeField] private float interpolationDuration = 0.35f;

    [Header("Reset (opcional)")]
    [SerializeField] private bool resetToDefaultOnNoPose = false;
    [SerializeField] private Transform defaultSpot;

    private Coroutine moveRoutine;
    private bool hasPendingMove;
    private Transform pendingTargetSpot;

    private void OnEnable()
    {
        if (!hasPendingMove || pendingTargetSpot == null)
        {
            return;
        }

        Transform target = pendingTargetSpot;
        hasPendingMove = false;
        pendingTargetSpot = null;
        MoveRigTo(target);
    }

    public override void ResetHoldEffects()
    {
        if (!resetToDefaultOnNoPose)
        {
            return;
        }

        MoveRigTo(defaultSpot);
    }

    public override void PreviewOption(int selectedOptionIndex)
    {
        MoveRigToOption(selectedOptionIndex);
    }

    protected override void ApplyCorrectEffect()
    {
        MoveRigToOption(CorrectOptionIndex);
    }

    protected override void ApplyWrongEffect1()
    {
        MoveRigToOption(WrongOption1Index);
    }

    protected override void ApplyWrongEffect2()
    {
        MoveRigToOption(WrongOption2Index);
    }

    private void MoveRigToOption(int optionIndex)
    {
        if (cameraOptionSpots == null || optionIndex < 0 || optionIndex >= cameraOptionSpots.Length)
        {
            return;
        }

        MoveRigTo(cameraOptionSpots[optionIndex]);
    }

    private void MoveRigTo(Transform targetSpot)
    {
        if (cameraRig == null || targetSpot == null)
        {
            return;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            hasPendingMove = true;
            pendingTargetSpot = targetSpot;
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(InterpolateToTarget(targetSpot));
    }

    private System.Collections.IEnumerator InterpolateToTarget(Transform targetSpot)
    {
        Vector3 startPos = cameraRig.position;
        Quaternion startRot = cameraRig.rotation;

        if (interpolationDuration <= 0f)
        {
            cameraRig.position = targetSpot.position;
            cameraRig.rotation = targetSpot.rotation;
            moveRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < interpolationDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / interpolationDuration);
            cameraRig.position = Vector3.Lerp(startPos, targetSpot.position, k);
            cameraRig.rotation = Quaternion.Slerp(startRot, targetSpot.rotation, k);
            yield return null;
        }

        cameraRig.position = targetSpot.position;
        cameraRig.rotation = targetSpot.rotation;
        moveRoutine = null;
    }
}

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using mptcc = Mediapipe.Tasks.Components.Containers;

public class CustomPoseDetector : MonoBehaviour
{
    public enum PosePresetType
    {
        TPose,
        APose,
        YPose
    }

    [System.Serializable]
    public class PoseState
    {
        [HideInInspector] public float currentHoldTimer = 0f;
        [HideInInspector] public bool isMatched = false;
        [HideInInspector] public bool eventFired = false;
    }

    [System.Serializable]
    public class JointAngleCondition
    {
        public enum PresetJoint
        {
            LeftArm,       // 11(Shoulder) - 13(Elbow) - 15(Wrist)
            RightArm,      // 12(Shoulder) - 14(Elbow) - 16(Wrist)
            LeftShoulder,  // 23(Hip) - 11(Shoulder) - 13(Elbow)
            RightShoulder, // 24(Hip) - 12(Shoulder) - 14(Elbow)
            LeftLeg,       // 23(Hip) - 25(Knee) - 27(Ankle)
            RightLeg,      // 24(Hip) - 26(Knee) - 28(Ankle)
            Custom
        }

        public PresetJoint jointType;
        [Tooltip("Only used if jointType is Custom")]
        public int customJointA = 11;
        [Tooltip("Middle/Vertex Joint (Only used if Custom)")]
        public int customJointB = 13;
        public int customJointC = 15;

        [Range(0f, 180f)]
        public float targetAngle = 180f;

        public void GetIndices(out int a, out int b, out int c)
        {
            switch (jointType)
            {
                case PresetJoint.LeftArm:       a = 11; b = 13; c = 15; break;
                case PresetJoint.RightArm:      a = 12; b = 14; c = 16; break;
                case PresetJoint.LeftShoulder:  a = 23; b = 11; c = 13; break;
                case PresetJoint.RightShoulder: a = 24; b = 12; c = 14; break;
                case PresetJoint.LeftLeg:       a = 23; b = 25; c = 27; break;
                case PresetJoint.RightLeg:      a = 24; b = 26; c = 28; break;
                default: a = customJointA; b = customJointB; c = customJointC; break;
            }
        }
    }

    [Header("Dependencies")]
    public AnswerHandler answerHandler;
    public InteractionUIManager uiManager;
    [Tooltip("Target annotation script to change connection colors (e.g. PoseLandmarkListAnnotation or MultiPoseLandmarkListWithMaskAnnotation)")]
    public MonoBehaviour targetAnnotation;

    private InteractionActionBase currentInteraction;

    [Header("Settings")]
    public Color defaultConnectionColor = Color.white;
    public Color matchConnectionColor = Color.green;
    [SerializeField] private float holdTime = 1.0f;
    [SerializeField] private float marginDegrees = 20f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [System.Serializable]
    public class ColorEvent : UnityEvent<Color> { }

    [System.Serializable]
    public class PoseConfirmedEvent : UnityEvent<InteractionType, int, string> { }

    [Header("Optional Output Events")]
    public UnityEvent onAnyPoseMatched;
    public UnityEvent onAllPosesLost;
    public ColorEvent onColorChangeRequested; // Connect this to SetConnectionColor externally if needed
    public PoseConfirmedEvent onPoseHoldConfirmed;

    private bool anyWasMatched = false;
    private float nextDebugLogTime = 0f;
    private bool warnedNoPoses = false;
    private int currentActiveOptionIndex = -1;
    private List<PoseState> poseStates = new List<PoseState>();

    private bool CanLogDebug()
    {
        if (!debugLogs)
        {
            return false;
        }

        if (Time.unscaledTime < nextDebugLogTime)
        {
            return false;
        }

        nextDebugLogTime = Time.unscaledTime + 1f;
        return true;
    }

    private void LogDebug(string message)
    {
        if (CanLogDebug())
        {
            Debug.Log(message, this);
        }
    }

    // LiveStream callback can arrive off main thread.
    // We only enqueue data there and evaluate in Update (main thread).
    private readonly object pendingFrameLock = new object();
    private List<Vector3> pendingLandmarks;
    private bool pendingFrameAvailable;
    private bool pendingFrameValid;

    private void Start()
    {
        if (answerHandler == null)
            answerHandler = FindFirstObjectByType<AnswerHandler>();

        if (uiManager == null)
            uiManager = FindFirstObjectByType<InteractionUIManager>();

        ApplyColor(defaultConnectionColor);
        uiManager?.ClearHoldProgress();
    }

    public void SetCurrentInteraction(InteractionActionBase interaction)
    {
        currentInteraction = interaction;
        poseStates.Clear();
        currentActiveOptionIndex = -1;
        currentInteraction?.ResetHoldEffects();

        if (currentInteraction != null && currentInteraction.PoseOptions != null)
        {
            for (int i = 0; i < currentInteraction.PoseOptions.Length; i++)
            {
                poseStates.Add(new PoseState());
            }
            if (debugLogs)
            {
                Debug.Log($"{nameof(CustomPoseDetector)}: Interaction set with {currentInteraction.PoseOptions.Length} poses.", this);
            }
        }
    }

    private void Update()
    {
        List<Vector3> frameCopy = null;
        bool hasFrame = false;
        bool isValid = false;

        lock (pendingFrameLock)
        {
            if (pendingFrameAvailable)
            {
                hasFrame = true;
                isValid = pendingFrameValid;

                if (pendingLandmarks != null)
                {
                    frameCopy = new List<Vector3>(pendingLandmarks);
                }

                pendingFrameAvailable = false;
            }
        }

        if (!hasFrame)
        {
            return;
        }

        if (!isValid || frameCopy == null || frameCopy.Count < 33)
        {
            ResetAllHolds();
            return;
        }

        EvaluatePoses(frameCopy);
    }



    public void ProcessLandmarks(mptcc.NormalizedLandmarks landmarks)
    {
        if (landmarks.landmarks == null || landmarks.landmarks.Count < 33)
        {
            lock (pendingFrameLock)
            {
                pendingFrameValid = false;
                pendingLandmarks = null;
                pendingFrameAvailable = true;
            }
            return;
        }

        var snapshot = new List<Vector3>(landmarks.landmarks.Count);
        for (int i = 0; i < landmarks.landmarks.Count; i++)
        {
            var l = landmarks.landmarks[i];
            snapshot.Add(new Vector3(l.x, l.y, l.z));
        }

        lock (pendingFrameLock)
        {
            pendingFrameValid = true;
            pendingLandmarks = snapshot;
            pendingFrameAvailable = true;
        }
    }

    // Overload for multiple targets
    public void ProcessLandmarksList(IReadOnlyList<mptcc.NormalizedLandmarks> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            lock (pendingFrameLock)
            {
                pendingFrameValid = false;
                pendingLandmarks = null;
                pendingFrameAvailable = true;
            }
            return;
        }

        // Just evaluate the first detected person
        ProcessLandmarks(targets[0]);
    }

    private void EvaluatePoses(IReadOnlyList<Vector3> poseData)
    {
        if (currentInteraction == null || currentInteraction.PoseOptions == null || currentInteraction.PoseOptions.Length == 0)
        {
            if (debugLogs && !warnedNoPoses)
            {
                Debug.LogWarning($"{nameof(CustomPoseDetector)}: No poses configured. Assign currentInteraction with PoseOptions.", this);
                warnedNoPoses = true;
            }
            ResetAllHolds();
            return;
        }



        bool currentAnyMatched = false;
        float highestProgress = 0f;
        int activeOptionIndex = -1;
        int bestPoseIndex = -1;
        string bestPoseName = string.Empty;
        float bestPoseAverageDelta = float.MaxValue;
        string bestPoseDetail = string.Empty;

        PoseData[] poses = currentInteraction.PoseOptions;

        for (int i = 0; i < poses.Length; i++)
        {
            var pose = poses[i];
            var state = poseStates[i];

            bool match = true;
            float totalDelta = 0f;
            int conditionCount = 0;
            string failDetail = string.Empty;
            string scoreDetail = string.Empty;

            foreach (var cond in pose.conditions)
            {
                cond.GetIndices(out int iA, out int iB, out int iC);

                if (iA < 0 || iA >= poseData.Count ||
                    iB < 0 || iB >= poseData.Count ||
                    iC < 0 || iC >= poseData.Count)
                {
                    match = false;
                    failDetail = $"Índices inválidos ({iA},{iB},{iC}) para landmarks Count={poseData.Count}";
                    break;
                }
                
                var lA = poseData[iA];
                var lB = poseData[iB];
                var lC = poseData[iC];

                float angle = CalculateAngle(lA, lB, lC);
                float delta = Mathf.Abs(angle - cond.targetAngle);
                totalDelta += delta;
                conditionCount++;
                scoreDetail += $"[{cond.jointType}: {angle:F1}/{cond.targetAngle:F1} Δ{delta:F1}] ";

                if (delta > marginDegrees)
                {
                    match = false;
                    if (string.IsNullOrEmpty(failDetail))
                    {
                        failDetail = $"{cond.jointType}: angle={angle:F1}, target={cond.targetAngle:F1}, delta={delta:F1}, margin={marginDegrees:F1}";
                    }
                    break;
                }
            }

            float avgDelta = conditionCount > 0 ? totalDelta / conditionCount : float.MaxValue;
            string poseDetail = string.IsNullOrEmpty(failDetail) ? "all conditions within margin" : failDetail;

            if (avgDelta < bestPoseAverageDelta)
            {
                bestPoseAverageDelta = avgDelta;
                bestPoseIndex = i;
                bestPoseName = pose.poseName;
                bestPoseDetail = poseDetail;
            }

            LogDebug($"{nameof(CustomPoseDetector)}: Pose[{i}] '{pose.poseName}' match={match} avgDelta={avgDelta:F1} detail='{poseDetail}' score='{scoreDetail}'");

            if (match)
            {
                currentAnyMatched = true;
                state.isMatched = true;
                state.currentHoldTimer += Time.deltaTime;

                float normalizedProgress = holdTime <= 0f
                    ? 1f
                    : Mathf.Clamp01(state.currentHoldTimer / holdTime);
                if (normalizedProgress > highestProgress)
                {
                    highestProgress = normalizedProgress;
                    activeOptionIndex = i;
                }

                if (state.currentHoldTimer >= holdTime && !state.eventFired)
                {
                    state.eventFired = true;
                    ConfirmPoseSelection(i);
                }
            }
            else
            {
                state.isMatched = false;
                state.currentHoldTimer = 0f;
                state.eventFired = false;
            }
        }

        if (activeOptionIndex >= 0)
        {
            if (activeOptionIndex != currentActiveOptionIndex)
            {
                currentInteraction?.PreviewOption(activeOptionIndex);
                currentActiveOptionIndex = activeOptionIndex;
            }

            uiManager?.SetHoldProgressForOption(activeOptionIndex, highestProgress);
        }
        else
        {
            if (currentActiveOptionIndex != -1)
            {
                currentInteraction?.ResetHoldEffects();
                currentActiveOptionIndex = -1;
            }

            uiManager?.ClearHoldProgress();
        }

        LogDebug($"{nameof(CustomPoseDetector)}: Closest pose: index={bestPoseIndex}, name='{bestPoseName}', avgDelta={bestPoseAverageDelta:F1}, detail='{bestPoseDetail}'");

        if (currentAnyMatched)
        {
            LogDebug($"{nameof(CustomPoseDetector)}: Pose matched. Progress: {highestProgress:F2}");
        }

        // Handle Visuals/Color changing
        if (currentAnyMatched && !anyWasMatched)
        {
            ApplyColor(matchConnectionColor);
            onAnyPoseMatched?.Invoke();
            anyWasMatched = true;
        }
        else if (!currentAnyMatched && anyWasMatched)
        {
            ApplyColor(defaultConnectionColor);
            onAllPosesLost?.Invoke();
            anyWasMatched = false;
        }
    }

    private void ResetAllHolds()
    {
        foreach (var state in poseStates)
        {
            state.isMatched = false;
            state.currentHoldTimer = 0f;
            state.eventFired = false;
        }

        uiManager?.ClearHoldProgress();

        if (anyWasMatched)
        {
            ApplyColor(defaultConnectionColor);
            onAllPosesLost?.Invoke();
            anyWasMatched = false;
        }
    }

    private float CalculateAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector2 vA = new Vector2(a.x, a.y);
        Vector2 vB = new Vector2(b.x, b.y);
        Vector2 vC = new Vector2(c.x, c.y);

        Vector2 vector1 = vA - vB;
        Vector2 vector2 = vC - vB;

        return Vector2.Angle(vector1, vector2);
    }

    private void ApplyColor(Color color)
    {
        onColorChangeRequested?.Invoke(color);

        if (targetAnnotation != null)
        {
            // We use SendMessage so you don't need to tightly couple with the specific homuler annotation class name
            targetAnnotation.SendMessage("SetConnectionColor", color, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void ConfirmPoseSelection(int index)
    {
        if (currentInteraction == null || currentInteraction.PoseOptions == null)
        {
            Debug.LogWarning($"{nameof(CustomPoseDetector)}: ConfirmPoseSelection called without a valid current interaction.", this);
            return;
        }

        if (index < 0 || index >= currentInteraction.PoseOptions.Length)
        {
            Debug.LogWarning($"{nameof(CustomPoseDetector)}: ConfirmPoseSelection index {index} is out of range for PoseOptions length {currentInteraction.PoseOptions.Length}.", this);
            return;
        }

        var pose = currentInteraction.PoseOptions[index];
        onPoseHoldConfirmed?.Invoke(currentInteraction.InteractionType, index, pose.poseName);

        if (answerHandler != null)
        {
            answerHandler.SubmitAnswer(currentInteraction.InteractionType, index);
            LogDebug($"Pose '{pose.poseName}' held. Option {index} submitted.");
        }
        else
        {
            Debug.LogWarning($"{nameof(CustomPoseDetector)}: Pose held but AnswerHandler not assigned.", this);
        }
    }
}

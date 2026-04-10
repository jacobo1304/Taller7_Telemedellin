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
    public class CustomPoseConfig
    {
        public string poseName = "New Pose";
        [Tooltip("Interaction type to send to AnswerHandler when pose is matched")]
        public InteractionType interactionType = InteractionType.Titulares;
        [Tooltip("Option index to send to AnswerHandler (0, 1, 2...) ")]
        public int selectedOptionIndex = 0;
        public PosePresetType presetType = PosePresetType.TPose;

        [Tooltip("How long must the pose be held (seconds) before firing the event?")]
        public float holdTime = 1.0f;

        [Header("Angle Conditions")]
        [Tooltip("Margin of error in degrees")]
        public float marginDegrees = 20f;
        public List<JointAngleCondition> conditions = new List<JointAngleCondition>();
        
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

    [Header("Settings")]
    public Color defaultConnectionColor = Color.white;
    public Color matchConnectionColor = Color.green;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugAngleDetails = false;
    [SerializeField] private bool debugPoseScores = false;
    [SerializeField, Min(0.1f)] private float debugLogInterval = 0.5f;

    [Header("Detection")]
    [SerializeField] private bool use3DAngles = true;

    [Header("Poses")]
    public List<CustomPoseConfig> customPoses = new List<CustomPoseConfig>();

    [Header("Preset Builder (T/A/Y)")]
    [SerializeField] private InteractionType presetInteractionType = InteractionType.Titulares;
    [SerializeField] private int tPoseOptionIndex = 0;
    [SerializeField] private int aPoseOptionIndex = 1;
    [SerializeField] private int yPoseOptionIndex = 2;
    [SerializeField] private float presetHoldTime = 1.0f;
    [SerializeField] private float presetMarginDegrees = 22f;

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
    private bool warnedNoLandmarkFeed = false;
    private float startedAtTime;

    // LiveStream callback can arrive off main thread.
    // We only enqueue data there and evaluate in Update (main thread).
    private readonly object pendingFrameLock = new object();
    private List<Vector3> pendingLandmarks;
    private bool pendingFrameAvailable;
    private bool pendingFrameValid;

    private void Start()
    {
        startedAtTime = Time.time;

        if (answerHandler == null)
            answerHandler = FindFirstObjectByType<AnswerHandler>();

        if (uiManager == null)
            uiManager = FindFirstObjectByType<InteractionUIManager>();

        if (debugLogs || debugAngleDetails || debugPoseScores)
        {
            Debug.Log($"{nameof(CustomPoseDetector)} started. Poses configured: {customPoses.Count}. use3DAngles={use3DAngles}", this);
        }

        ApplyColor(defaultConnectionColor);
        uiManager?.ClearHoldProgress();
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

    [ContextMenu("Create T/A/Y Presets (same question)")]
    public void CreateTAYPresets()
    {
        customPoses = PosePresetLibrary.CreateTayPresets(
            presetInteractionType,
            tPoseOptionIndex,
            aPoseOptionIndex,
            yPoseOptionIndex,
            presetHoldTime,
            presetMarginDegrees);
    }

    // Call this from MediaPipe tasks, e.g. PoseDetection graph
    public void ProcessLandmarks(mptcc.NormalizedLandmarks landmarks)
    {
        warnedNoLandmarkFeed = false;

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
            warnedNoLandmarkFeed = true;
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
        if (customPoses == null || customPoses.Count == 0)
        {
            if ((debugLogs || debugAngleDetails || debugPoseScores) && !warnedNoPoses)
            {
                Debug.LogWarning($"{nameof(CustomPoseDetector)}: No poses configured. Use 'Create T/A/Y Presets' or fill customPoses in Inspector.", this);
                warnedNoPoses = true;
            }
            ResetAllHolds();
            return;
        }

        if (warnedNoLandmarkFeed && (debugLogs || debugAngleDetails || debugPoseScores) && (Time.time - startedAtTime) > 2f && Time.time >= nextDebugLogTime)
        {
            Debug.LogWarning($"{nameof(CustomPoseDetector)}: Latest callback had 0 targets (persona no detectada o feed intermitente).", this);
            nextDebugLogTime = Time.time + debugLogInterval;
            warnedNoLandmarkFeed = false;
        }

        bool currentAnyMatched = false;
        float highestProgress = 0f;
        int activeOptionIndex = -1;
        string bestPoseName = string.Empty;
        float bestPoseAverageDelta = float.MaxValue;
        string bestPoseDetail = string.Empty;

        foreach (var pose in customPoses)
        {
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

                if (delta > pose.marginDegrees)
                {
                    match = false;
                    if (string.IsNullOrEmpty(failDetail))
                    {
                        failDetail = $"{cond.jointType}: angle={angle:F1}, target={cond.targetAngle:F1}, delta={delta:F1}, margin={pose.marginDegrees:F1}";
                    }
                    break;
                }
            }

            float avgDelta = conditionCount > 0 ? totalDelta / conditionCount : float.MaxValue;
            if (avgDelta < bestPoseAverageDelta)
            {
                bestPoseAverageDelta = avgDelta;
                bestPoseName = pose.poseName;
                bestPoseDetail = string.IsNullOrEmpty(failDetail) ? "all conditions within margin" : failDetail;
            }

            if (debugPoseScores && Time.time >= nextDebugLogTime)
            {
                Debug.Log($"PoseScore '{pose.poseName}' match={match} hold={pose.currentHoldTimer:F2}/{pose.holdTime:F2} avgΔ={avgDelta:F1} :: {scoreDetail}", this);
            }

            if (match)
            {
                currentAnyMatched = true;
                pose.isMatched = true;
                pose.currentHoldTimer += Time.deltaTime;

                float normalizedProgress = pose.holdTime <= 0f
                    ? 1f
                    : Mathf.Clamp01(pose.currentHoldTimer / pose.holdTime);
                if (normalizedProgress > highestProgress)
                {
                    highestProgress = normalizedProgress;
                    activeOptionIndex = pose.selectedOptionIndex;
                }

                if (pose.currentHoldTimer >= pose.holdTime && !pose.eventFired)
                {
                    pose.eventFired = true;
                    ConfirmPoseSelection(pose);
                }
            }
            else
            {
                pose.isMatched = false;
                pose.currentHoldTimer = 0f;
                pose.eventFired = false;
            }
        }

        if (highestProgress > 0f && activeOptionIndex >= 0)
        {
            uiManager?.SetHoldProgressForOption(activeOptionIndex, highestProgress);
        }
        else
        {
            uiManager?.ClearHoldProgress();
        }

        if ((debugLogs || debugAngleDetails || debugPoseScores) && Time.time >= nextDebugLogTime)
        {
            if (currentAnyMatched)
            {
                Debug.Log($"{nameof(CustomPoseDetector)}: pose candidate matched. Hold progress={highestProgress:F2}", this);
            }
            else
            {
                if (debugAngleDetails)
                {
                    Debug.Log($"{nameof(CustomPoseDetector)}: no pose matched. Closest='{bestPoseName}' | {bestPoseDetail}", this);
                }
                else
                {
                    Debug.Log($"{nameof(CustomPoseDetector)}: no pose matched. Closest='{bestPoseName}'", this);
                }
            }

            nextDebugLogTime = Time.time + debugLogInterval;
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
        foreach (var pose in customPoses)
        {
            pose.isMatched = false;
            pose.currentHoldTimer = 0f;
            pose.eventFired = false;
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
        if (use3DAngles)
        {
            Vector3 vector13 = a - b;
            Vector3 vector23 = c - b;
            return Vector3.Angle(vector13, vector23);
        }

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

    private void ConfirmPoseSelection(CustomPoseConfig pose)
    {
        onPoseHoldConfirmed?.Invoke(pose.interactionType, pose.selectedOptionIndex, pose.poseName);

        if (answerHandler != null)
        {
            answerHandler.SubmitAnswer(pose.interactionType, pose.selectedOptionIndex);
            if (debugLogs || debugAngleDetails || debugPoseScores)
            {
                Debug.Log($"Pose '{pose.poseName}' HOLD complete. Sent interaction={pose.interactionType}, option={pose.selectedOptionIndex}", this);
            }
        }
        else if (debugLogs || debugAngleDetails || debugPoseScores)
        {
            Debug.LogWarning($"{nameof(CustomPoseDetector)}: Hold complete but AnswerHandler is not assigned.", this);
        }
    }
}

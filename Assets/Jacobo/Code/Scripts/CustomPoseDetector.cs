using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using mptcc = Mediapipe.Tasks.Components.Containers;

public class CustomPoseDetector : MonoBehaviour
{
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
    [SerializeField, Min(0f)] private float holdTimeSeconds = 1.0f;
    [SerializeField, Min(0f)] private float defaultMarginDegrees = 20f;
    [SerializeField, Min(0f)] private float matchLossGraceSeconds = 0.2f;

    [Header("Current Interaction (runtime)")]
    [SerializeField] private InteractionActionBase currentInteraction;

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
    private bool warnedNoPosesInInteraction = false;
    private bool warnedNoLandmarkFeed = false;
    private float startedAtTime;

    // LiveStream callback can arrive off main thread.
    // We only enqueue data there and evaluate in Update (main thread).
    private readonly object pendingFrameLock = new object();
    private List<Vector3> pendingLandmarks;
    private bool pendingFrameAvailable;
    private bool pendingFrameValid;
    private float[] holdTimers = new float[0];
    private bool[] holdEventsFired = new bool[0];
    private bool[] poseMatchedThisFrame = new bool[0];
    private bool responseLocked = false;
    private float lastStrictMatchTime = float.NegativeInfinity;
    private int lastMatchedOptionIndex = -1;
    private float lastMatchedProgress = 0f;

    private void Start()
    {
        startedAtTime = Time.time;

        if (answerHandler == null)
            answerHandler = FindFirstObjectByType<AnswerHandler>();

        if (uiManager == null)
            uiManager = FindFirstObjectByType<InteractionUIManager>();

        if (currentInteraction == null)
        {
            currentInteraction = FindFirstObjectByType<InteractionActionBase>();
        }

        if (debugLogs || debugAngleDetails || debugPoseScores)
        {
            int poseCount = currentInteraction?.PoseOptions == null ? 0 : currentInteraction.PoseOptions.Length;
            Debug.Log($"{nameof(CustomPoseDetector)} started. interaction={(currentInteraction == null ? "null" : currentInteraction.InteractionType.ToString())}, poses={poseCount}, use3DAngles={use3DAngles}", this);
        }

        ApplyColor(defaultConnectionColor);
        uiManager?.ClearHoldProgress();
        ResetAllHolds();
    }

    public void SetCurrentInteraction(InteractionActionBase interaction)
    {
        responseLocked = false;
        currentInteraction = interaction;
        warnedNoPosesInInteraction = false;
        ResetAllHolds();

        if (debugLogs || debugAngleDetails || debugPoseScores)
        {
            int poseCount = currentInteraction?.PoseOptions == null ? 0 : currentInteraction.PoseOptions.Length;
            Debug.Log($"{nameof(CustomPoseDetector)}: current interaction changed to {(currentInteraction == null ? "null" : currentInteraction.InteractionType.ToString())}. poseCount={poseCount}", this);
        }
    }

    public void SetResponseLock(bool locked)
    {
        responseLocked = locked;

        // While locked we stop hold visuals, but we do not clear previews/effects,
        // so selected answer remains frozen until next interaction.
        if (responseLocked)
        {
            for (int i = 0; i < holdTimers.Length; i++)
            {
                holdTimers[i] = 0f;
                holdEventsFired[i] = false;
            }

            uiManager?.ClearHoldProgress();
        }
    }

    private void Update()
    {
        if (responseLocked)
        {
            lock (pendingFrameLock)
            {
                pendingFrameAvailable = false;
            }
            return;
        }

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
            HandleMatchLossWithGrace();
            return;
        }

        EvaluatePoses(frameCopy);
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
        if (currentInteraction == null)
        {
            if ((debugLogs || debugAngleDetails || debugPoseScores) && Time.time >= nextDebugLogTime)
            {
                Debug.LogWarning($"{nameof(CustomPoseDetector)}: No current interaction assigned.", this);
                nextDebugLogTime = Time.time + debugLogInterval;
            }
            ResetAllHolds();
            return;
        }

        PoseData[] poseOptions = currentInteraction.PoseOptions;
        if (poseOptions == null || poseOptions.Length == 0)
        {
            if ((debugLogs || debugAngleDetails || debugPoseScores) && !warnedNoPosesInInteraction)
            {
                Debug.LogWarning($"{nameof(CustomPoseDetector)}: Current interaction has no PoseOptions configured.", this);
                warnedNoPosesInInteraction = true;
            }
            ResetAllHolds();
            return;
        }

        EnsureRuntimePoseState(poseOptions.Length);

        for (int i = 0; i < poseMatchedThisFrame.Length; i++)
        {
            poseMatchedThisFrame[i] = false;
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

        for (int poseIndex = 0; poseIndex < poseOptions.Length; poseIndex++)
        {
            PoseData pose = poseOptions[poseIndex];
            if (pose == null || pose.conditions == null || pose.conditions.Count == 0)
            {
                holdTimers[poseIndex] = 0f;
                holdEventsFired[poseIndex] = false;
                continue;
            }

            bool match = true;
            float totalDelta = 0f;
            int conditionCount = 0;
            string failDetail = string.Empty;
            string scoreDetail = string.Empty;
            float margin = pose.marginDegrees > 0f ? pose.marginDegrees : defaultMarginDegrees;

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

                if (delta > margin)
                {
                    match = false;
                    if (string.IsNullOrEmpty(failDetail))
                    {
                        failDetail = $"{cond.jointType}: angle={angle:F1}, target={cond.targetAngle:F1}, delta={delta:F1}, margin={margin:F1}";
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
                Debug.Log($"PoseScore '{pose.poseName}' idx={poseIndex} match={match} hold={holdTimers[poseIndex]:F2}/{holdTimeSeconds:F2} avgΔ={avgDelta:F1} :: {scoreDetail}", this);
            }

            if (match)
            {
                currentAnyMatched = true;
                poseMatchedThisFrame[poseIndex] = true;
            }
        }

        if (currentAnyMatched)
        {
            lastStrictMatchTime = Time.time;

            for (int poseIndex = 0; poseIndex < poseOptions.Length; poseIndex++)
            {
                if (poseMatchedThisFrame[poseIndex])
                {
                    holdTimers[poseIndex] += Time.deltaTime;

                    float normalizedProgress = holdTimeSeconds <= 0f
                        ? 1f
                        : Mathf.Clamp01(holdTimers[poseIndex] / holdTimeSeconds);

                    if (normalizedProgress > highestProgress)
                    {
                        highestProgress = normalizedProgress;
                        activeOptionIndex = poseIndex;
                    }

                    if (holdTimers[poseIndex] >= holdTimeSeconds && !holdEventsFired[poseIndex])
                    {
                        holdEventsFired[poseIndex] = true;
                        ConfirmPoseSelection(poseIndex, poseOptions[poseIndex]);
                        return;
                    }
                }
                else
                {
                    holdTimers[poseIndex] = 0f;
                    holdEventsFired[poseIndex] = false;
                }
            }

            if (activeOptionIndex >= 0)
            {
                lastMatchedOptionIndex = activeOptionIndex;
                lastMatchedProgress = highestProgress;
            }
        }

        bool keepMatchByGrace = !currentAnyMatched && IsWithinMatchLossGrace();
        bool effectiveAnyMatched = currentAnyMatched || keepMatchByGrace;

        if (currentAnyMatched && highestProgress > 0f && activeOptionIndex >= 0)
        {
            uiManager?.SetHoldProgressForOption(activeOptionIndex, highestProgress);
            answerHandler?.PreviewSelection(currentInteraction.InteractionType, activeOptionIndex);
        }
        else if (keepMatchByGrace && lastMatchedOptionIndex >= 0 && lastMatchedProgress > 0f)
        {
            uiManager?.SetHoldProgressForOption(lastMatchedOptionIndex, lastMatchedProgress);
            answerHandler?.PreviewSelection(currentInteraction.InteractionType, lastMatchedOptionIndex);
        }
        else
        {
            uiManager?.ClearHoldProgress();
            answerHandler?.ClearAllPreviews();
        }

        if ((debugLogs || debugAngleDetails || debugPoseScores) && Time.time >= nextDebugLogTime)
        {
            if (currentAnyMatched)
            {
                Debug.Log($"{nameof(CustomPoseDetector)}: pose candidate matched. Hold progress={highestProgress:F2}", this);
            }
            else if (keepMatchByGrace)
            {
                float graceRemaining = Mathf.Max(0f, matchLossGraceSeconds - (Time.time - lastStrictMatchTime));
                Debug.Log($"{nameof(CustomPoseDetector)}: pose momentáneamente perdida. Manteniendo match por gracia ({graceRemaining:F2}s restantes).", this);
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
        if (effectiveAnyMatched && !anyWasMatched)
        {
            ApplyColor(matchConnectionColor);
            onAnyPoseMatched?.Invoke();
            anyWasMatched = true;
        }
        else if (!effectiveAnyMatched && anyWasMatched)
        {
            ApplyColor(defaultConnectionColor);
            onAllPosesLost?.Invoke();
            anyWasMatched = false;
        }
    }

    private void ResetAllHolds()
    {
        for (int i = 0; i < holdTimers.Length; i++)
        {
            holdTimers[i] = 0f;
            holdEventsFired[i] = false;
        }

        lastStrictMatchTime = float.NegativeInfinity;
        lastMatchedOptionIndex = -1;
        lastMatchedProgress = 0f;

        uiManager?.ClearHoldProgress();
        answerHandler?.ClearAllPreviews();

        if (anyWasMatched)
        {
            ApplyColor(defaultConnectionColor);
            onAllPosesLost?.Invoke();
            anyWasMatched = false;
        }
    }

    private void EnsureRuntimePoseState(int count)
    {
        if (holdTimers.Length == count && holdEventsFired.Length == count && poseMatchedThisFrame.Length == count)
        {
            return;
        }

        holdTimers = new float[count];
        holdEventsFired = new bool[count];
        poseMatchedThisFrame = new bool[count];
    }

    private bool IsWithinMatchLossGrace()
    {
        if (matchLossGraceSeconds <= 0f)
        {
            return false;
        }

        if (!anyWasMatched)
        {
            return false;
        }

        return (Time.time - lastStrictMatchTime) <= matchLossGraceSeconds;
    }

    private void HandleMatchLossWithGrace()
    {
        if (IsWithinMatchLossGrace())
        {
            if (lastMatchedOptionIndex >= 0 && lastMatchedProgress > 0f)
            {
                uiManager?.SetHoldProgressForOption(lastMatchedOptionIndex, lastMatchedProgress);

                if (currentInteraction != null)
                {
                    answerHandler?.PreviewSelection(currentInteraction.InteractionType, lastMatchedOptionIndex);
                }
            }

            return;
        }

        ResetAllHolds();
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

    private void ConfirmPoseSelection(int selectedOptionIndex, PoseData pose)
    {
        InteractionType interactionType = currentInteraction != null ? currentInteraction.InteractionType : InteractionType.Titulares;
        string poseName = pose != null ? pose.poseName : $"Pose {selectedOptionIndex}";

        onPoseHoldConfirmed?.Invoke(interactionType, selectedOptionIndex, poseName);

        if (answerHandler != null)
        {
            answerHandler.SubmitAnswer(interactionType, selectedOptionIndex);
            if (debugLogs || debugAngleDetails || debugPoseScores)
            {
                Debug.Log($"Pose '{poseName}' HOLD complete. Sent interaction={interactionType}, option={selectedOptionIndex}", this);
            }
        }
        else if (debugLogs || debugAngleDetails || debugPoseScores)
        {
            Debug.LogWarning($"{nameof(CustomPoseDetector)}: Hold complete but AnswerHandler is not assigned.", this);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using mptcc = Mediapipe.Tasks.Components.Containers;

// Note: Confirmation branching (yes/no by index 0/1) was moved to TwoPoseListener.
public class SimplePoseListener : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InteractionUIManager uiManager;
    [SerializeField] private CustomPoseDetector customPoseDetector;
    [SerializeField] private TwoPoseListener twoPoseListener;
    [Tooltip("Target annotation script to change connection colors (e.g. PoseLandmarkListAnnotation or MultiPoseLandmarkListWithMaskAnnotation)")]
    [SerializeField] private MonoBehaviour targetAnnotation;

    [Header("Joint Color")]
    [SerializeField] private Color defaultConnectionColor = Color.white;
    [SerializeField] private Color matchConnectionColor = Color.green;

    [Header("Pose Targets (1 o más)")]
    [SerializeField] private PoseData[] targetPoses = new PoseData[1];

    [Header("Detection")]
    [SerializeField] private bool use3DAngles = true;
    [SerializeField, Min(0f)] private float holdTimeSeconds = 1.0f;
    [SerializeField, Min(0f)] private float defaultMarginDegrees = 20f;

    [Header("UI Hold Fill")]
    [SerializeField] private bool useMatchedPoseIndexAsFillIndex = true;
    [SerializeField] private int fixedHoldFillIndex = 0;

    [Header("Custom UI (opcional para poses simples)")]
    [SerializeField] private bool useCustomSimpleUI = false;
    [Tooltip("Slots de imagen (uno por pose). Deben tener el mismo tamaño que customHoldFillImages y targetPoses.")]
    [SerializeField] private Image[] customPoseImageSlots = new Image[0];
    [Tooltip("Imágenes Fill (uno por pose). Deben tener el mismo tamaño que customPoseImageSlots y targetPoses.")]
    [SerializeField] private Image[] customHoldFillImages = new Image[0];
    [Tooltip("Si la pose no tiene sprite asignado, conserva el sprite actual del slot en vez de ocultarlo.")]
    [SerializeField] private bool keepSlotSpriteIfPoseSpriteMissing = true;

    [Header("Flow")]
    [SerializeField] private bool autoStopOnSuccess = true;
    [SerializeField] private bool lockCustomDetectorWhileListening = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onListeningStarted;
    [SerializeField] private UnityEvent onListeningStopped;
    [SerializeField] private UnityEvent onPoseDetected;
    [SerializeField] private UnityEvent onPoseLost;

    [System.Serializable]
    public class PoseResolvedEvent : UnityEvent<PoseData, int, string> { }
    [SerializeField] private PoseResolvedEvent onHoldCompleted;
    [SerializeField] private UnityEvent onHoldCompletedNoArgs;

    [Header("Optional Direct Action")]
    [SerializeField] private CinematicManager cinematicManager;
    [SerializeField] private bool callPlayNextOnHoldComplete = false;

    [System.Serializable]
    public class ColorEvent : UnityEvent<Color> { }
    [SerializeField] private ColorEvent onColorChangeRequested;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugAngleDetails = false;
    [SerializeField] private bool debugPoseScores = false;
    [SerializeField, Min(0.1f)] private float debugLogInterval = 0.5f;
    [SerializeField] private bool logSignalConfirmation = true;
    [SerializeField] private bool debugHoldEvents = true;

    private readonly object pendingFrameLock = new object();
    private List<Vector3> pendingLandmarks;
    private bool pendingFrameAvailable;
    private bool pendingFrameValid;

    private float[] holdTimers = new float[0];
    private bool isListening;
    private bool completed;
    private bool anyPoseMatched;
    private bool warnedNoPoses;
    private bool warnedNoLandmarkFeed;
    private float startedAtTime;
    private float nextDebugLogTime;
    private bool externallyPaused;

    public bool IsListening => isListening;
    public bool IsCompleted => completed;
    public bool IsExternallyPaused => externallyPaused;

    private void Awake()
    {
        startedAtTime = Time.time;

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<InteractionUIManager>();
        }

        if (customPoseDetector == null)
        {
            customPoseDetector = FindFirstObjectByType<CustomPoseDetector>();
        }

        if (twoPoseListener == null)
        {
            twoPoseListener = FindFirstObjectByType<TwoPoseListener>();
        }

        ClearAllHoldProgress();
        ApplyColor(defaultConnectionColor);

        if (useCustomSimpleUI)
        {
            BuildCustomUI();
        }
    }

    private void OnDisable()
    {
        StopListening(clearUI: true, unlockCustomDetector: true, invokeEvent: false);
    }

    public void AskForGenericInteraction()
    {
        if (logSignalConfirmation)
        {
            Debug.Log($"{nameof(SimplePoseListener)}: Signal recibido. AskForGenericInteraction() ejecutado.", this);
        }

        if (debugHoldEvents)
        {
            int persistentCount = onHoldCompleted == null ? 0 : onHoldCompleted.GetPersistentEventCount();
            Debug.Log($"{nameof(SimplePoseListener)}: onHoldCompleted persistent listeners={persistentCount}", this);
        }

        if (completed)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: Ya se completó. Llama ResetListener() para volver a escuchar.", this);
            }
            return;
        }

        if (targetPoses == null || targetPoses.Length == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: No hay poses configuradas en targetPoses.", this);
            }
            return;
        }

        isListening = true;
        externallyPaused = false;
        warnedNoPoses = false;
        warnedNoLandmarkFeed = false;
        anyPoseMatched = false;
        EnsureTimerState(targetPoses.Length);
        ResetTimersOnly();
        ApplyColor(defaultConnectionColor);

        if (useCustomSimpleUI)
        {
            BuildCustomUI();
        }

        if (lockCustomDetectorWhileListening)
        {
            customPoseDetector?.SetResponseLock(true);
        }

        twoPoseListener?.SetExternallyPaused(true);

        onListeningStarted?.Invoke();

        if (debugLogs)
        {
            Debug.Log($"{nameof(SimplePoseListener)}: Listening started with {targetPoses.Length} pose(s).", this);
        }
    }

    public void StopListening()
    {
        StopListening(clearUI: true, unlockCustomDetector: true, invokeEvent: true);
    }

    public void SetExternallyPaused(bool paused)
    {
        externallyPaused = paused;

        if (externallyPaused)
        {
            lock (pendingFrameLock)
            {
                pendingFrameAvailable = false;
            }
            ResetTimersOnly();
            ClearAllHoldProgress();
            ApplyColor(defaultConnectionColor);
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(SimplePoseListener)}: externallyPaused={(externallyPaused ? "TRUE" : "FALSE")}", this);
        }
    }

    public void ResetListener()
    {
        completed = false;
        StopListening(clearUI: true, unlockCustomDetector: true, invokeEvent: false);
    }

    public void SetTargetPoses(PoseData[] poses)
    {
        targetPoses = poses ?? new PoseData[0];
        EnsureTimerState(targetPoses.Length);
        ResetTimersOnly();

        if (useCustomSimpleUI && isListening)
        {
            BuildCustomUI();
        }
    }

    // Called from MediaPipe runner callbacks
    public void ProcessLandmarks(mptcc.NormalizedLandmarks landmarks)
    {
        if (landmarks.landmarks == null || landmarks.landmarks.Count < 33)
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

    // Overload for multi-target result
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

        ProcessLandmarks(targets[0]);
    }

    private void Update()
    {
        if (externallyPaused)
        {
            lock (pendingFrameLock)
            {
                pendingFrameAvailable = false;
            }
            return;
        }

        if (!isListening || completed)
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
            if ((debugLogs || debugAngleDetails || debugPoseScores) && Time.time >= nextDebugLogTime)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: frame inválido o incompleto. Se resetea hold.", this);
                nextDebugLogTime = Time.time + debugLogInterval;
            }
            ClearActiveMatchState();
            return;
        }

        EvaluateTargets(frameCopy);
    }

    private void EvaluateTargets(IReadOnlyList<Vector3> poseData)
    {
        if (targetPoses == null || targetPoses.Length == 0)
        {
            if (!warnedNoPoses && debugLogs)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: targetPoses vacío.", this);
                warnedNoPoses = true;
            }
            ClearActiveMatchState();
            return;
        }

        EnsureTimerState(targetPoses.Length);

        if (warnedNoLandmarkFeed && (debugLogs || debugAngleDetails || debugPoseScores) && (Time.time - startedAtTime) > 2f && Time.time >= nextDebugLogTime)
        {
            Debug.LogWarning($"{nameof(SimplePoseListener)}: Callback con 0 targets o feed intermitente.", this);
            warnedNoLandmarkFeed = false;
            nextDebugLogTime = Time.time + debugLogInterval;
        }

        bool currentAnyMatched = false;
        float highestProgress = 0f;
        int activePoseIndex = -1;
        string bestPoseName = string.Empty;
        float bestPoseAverageDelta = float.MaxValue;
        string bestPoseDetail = string.Empty;

        for (int i = 0; i < targetPoses.Length; i++)
        {
            PoseData pose = targetPoses[i];
            if (pose == null || pose.conditions == null || pose.conditions.Count == 0)
            {
                holdTimers[i] = 0f;
                continue;
            }

            bool match = true;
            float margin = pose.marginDegrees > 0f ? pose.marginDegrees : defaultMarginDegrees;
            float totalDelta = 0f;
            int conditionCount = 0;
            string failDetail = string.Empty;
            string scoreDetail = string.Empty;

            for (int c = 0; c < pose.conditions.Count; c++)
            {
                var cond = pose.conditions[c];
                cond.GetIndices(out int iA, out int iB, out int iC);

                if (iA < 0 || iA >= poseData.Count ||
                    iB < 0 || iB >= poseData.Count ||
                    iC < 0 || iC >= poseData.Count)
                {
                    match = false;
                    failDetail = $"Índices inválidos ({iA},{iB},{iC}) Count={poseData.Count}";
                    break;
                }

                float angle = CalculateAngle(poseData[iA], poseData[iB], poseData[iC]);
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
                Debug.Log($"SimplePoseScore '{pose.poseName}' idx={i} match={match} hold={holdTimers[i]:F2}/{holdTimeSeconds:F2} avgΔ={avgDelta:F1} :: {scoreDetail}", this);
            }

            if (match)
            {
                currentAnyMatched = true;
                holdTimers[i] += Time.deltaTime;

                float progress = holdTimeSeconds <= 0f ? 1f : Mathf.Clamp01(holdTimers[i] / holdTimeSeconds);
                if (progress > highestProgress)
                {
                    highestProgress = progress;
                    activePoseIndex = i;
                }

                if (holdTimers[i] >= holdTimeSeconds)
                {
                    if (debugHoldEvents)
                    {
                        Debug.Log($"{nameof(SimplePoseListener)}: Hold threshold reached for poseIndex={i}. timer={holdTimers[i]:F3}, required={holdTimeSeconds:F3}", this);
                    }
                    CompleteWithPose(i, pose);
                    return;
                }
            }
            else
            {
                holdTimers[i] = 0f;
            }
        }

        if (currentAnyMatched)
        {
            if (!anyPoseMatched)
            {
                onPoseDetected?.Invoke();
                ApplyColor(matchConnectionColor);
                anyPoseMatched = true;
            }

            int fillIndex = useMatchedPoseIndexAsFillIndex ? activePoseIndex : fixedHoldFillIndex;
            if (fillIndex < 0)
            {
                fillIndex = 0;
            }
            SetHoldProgress(fillIndex, highestProgress);
        }
        else
        {
            if (anyPoseMatched)
            {
                onPoseLost?.Invoke();
                ApplyColor(defaultConnectionColor);
                anyPoseMatched = false;
            }

            ClearAllHoldProgress();
        }

        if ((debugLogs || debugAngleDetails || debugPoseScores) && Time.time >= nextDebugLogTime)
        {
            if (currentAnyMatched)
            {
                Debug.Log($"{nameof(SimplePoseListener)}: pose candidate matched. Hold progress={highestProgress:F2}, activePoseIndex={activePoseIndex}", this);
            }
            else
            {
                if (debugAngleDetails)
                {
                    Debug.Log($"{nameof(SimplePoseListener)}: no pose matched. Closest='{bestPoseName}' | {bestPoseDetail}", this);
                }
                else
                {
                    Debug.Log($"{nameof(SimplePoseListener)}: no pose matched. Closest='{bestPoseName}'", this);
                }
            }

            nextDebugLogTime = Time.time + debugLogInterval;
        }
    }

    private void CompleteWithPose(int poseIndex, PoseData pose)
    {
        if (debugHoldEvents)
        {
            Debug.Log($"{nameof(SimplePoseListener)}: CompleteWithPose ENTER poseIndex={poseIndex}, poseName={(pose == null ? "null" : pose.poseName)}, isListening={isListening}, completed(before)={completed}", this);
        }

        completed = true;

        int fillIndex = useMatchedPoseIndexAsFillIndex ? poseIndex : fixedHoldFillIndex;
        if (fillIndex < 0)
        {
            fillIndex = 0;
        }

        SetHoldProgress(fillIndex, 1f);

        if (debugHoldEvents)
        {
            int persistentCount = onHoldCompleted == null ? 0 : onHoldCompleted.GetPersistentEventCount();
            if (persistentCount == 0)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: onHoldCompleted no tiene listeners persistentes asignados en Inspector.", this);
            }
            else
            {
                for (int i = 0; i < persistentCount; i++)
                {
                    var target = onHoldCompleted.GetPersistentTarget(i);
                    string method = onHoldCompleted.GetPersistentMethodName(i);
                    Debug.Log($"{nameof(SimplePoseListener)}: onHoldCompleted listener[{i}] target={(target == null ? "null" : target.name)} method={method}", this);
                }
            }

            Debug.Log($"{nameof(SimplePoseListener)}: Invoking onHoldCompleted...", this);
        }

        try
        {
            onHoldCompleted?.Invoke(pose, poseIndex, pose != null ? pose.poseName : $"Pose {poseIndex}");
            if (debugHoldEvents)
            {
                Debug.Log($"{nameof(SimplePoseListener)}: onHoldCompleted invoked successfully.", this);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex, this);
        }

        try
        {
            onHoldCompletedNoArgs?.Invoke();
            if (debugHoldEvents)
            {
                int noArgCount = onHoldCompletedNoArgs == null ? 0 : onHoldCompletedNoArgs.GetPersistentEventCount();
                Debug.Log($"{nameof(SimplePoseListener)}: onHoldCompletedNoArgs invoked. listeners={noArgCount}", this);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex, this);
        }

        if (callPlayNextOnHoldComplete)
        {
            if (cinematicManager == null)
            {
                cinematicManager = FindFirstObjectByType<CinematicManager>();
            }

            if (cinematicManager != null)
            {
                if (debugHoldEvents || debugLogs)
                {
                    Debug.Log($"{nameof(SimplePoseListener)}: Calling CinematicManager.PlayNext() directly.", this);
                }
                cinematicManager.PlayNext();
            }
            else if (debugHoldEvents || debugLogs)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: callPlayNextOnHoldComplete activo pero no se encontró CinematicManager.", this);
            }
        }

        if (autoStopOnSuccess)
        {
            StopListening(clearUI: false, unlockCustomDetector: true, invokeEvent: false);
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(SimplePoseListener)}: Hold complete for poseIndex={poseIndex}, poseName={(pose == null ? "null" : pose.poseName)}", this);
        }
    }

    private void StopListening(bool clearUI, bool unlockCustomDetector, bool invokeEvent)
    {
        bool wasListening = isListening;
        isListening = false;

        lock (pendingFrameLock)
        {
            pendingFrameAvailable = false;
        }

        ResetTimersOnly();

        if (clearUI)
        {
            ClearAllHoldProgress();
        }

        if (anyPoseMatched)
        {
            onPoseLost?.Invoke();
            ApplyColor(defaultConnectionColor);
            anyPoseMatched = false;
        }
        else
        {
            ApplyColor(defaultConnectionColor);
        }

        if (unlockCustomDetector && lockCustomDetectorWhileListening)
        {
            customPoseDetector?.SetResponseLock(false);
        }

        twoPoseListener?.SetExternallyPaused(false);

        if (wasListening && invokeEvent)
        {
            onListeningStopped?.Invoke();
        }
    }

    private void ClearActiveMatchState()
    {
        ResetTimersOnly();
        ClearAllHoldProgress();

        if (anyPoseMatched)
        {
            onPoseLost?.Invoke();
            ApplyColor(defaultConnectionColor);
            anyPoseMatched = false;
        }
        else
        {
            ApplyColor(defaultConnectionColor);
        }
    }

    private void EnsureTimerState(int count)
    {
        if (holdTimers.Length == count)
        {
            return;
        }

        holdTimers = new float[count];
    }

    private void ResetTimersOnly()
    {
        for (int i = 0; i < holdTimers.Length; i++)
        {
            holdTimers[i] = 0f;
        }
    }

    private float CalculateAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        if (use3DAngles)
        {
            return Vector3.Angle(a - b, c - b);
        }

        Vector2 vA = new Vector2(a.x, a.y);
        Vector2 vB = new Vector2(b.x, b.y);
        Vector2 vC = new Vector2(c.x, c.y);
        return Vector2.Angle(vA - vB, vC - vB);
    }

    private void SetHoldProgress(int optionIndex, float normalizedProgress)
    {
        if (!useCustomSimpleUI || !IsCustomUIReady())
        {
            uiManager?.SetHoldProgressForOption(optionIndex, normalizedProgress);
            return;
        }

        float progress = Mathf.Clamp01(normalizedProgress);
        for (int i = 0; i < customHoldFillImages.Length; i++)
        {
            Image fill = customHoldFillImages[i];
            if (fill == null)
            {
                continue;
            }

            bool isSelected = i == optionIndex;
            fill.gameObject.SetActive(isSelected && progress > 0f);
            fill.fillAmount = isSelected ? progress : 0f;
        }
    }

    private void ClearAllHoldProgress()
    {
        if (useCustomSimpleUI && customHoldFillImages != null && customHoldFillImages.Length > 0)
        {
            for (int i = 0; i < customHoldFillImages.Length; i++)
            {
                Image fill = customHoldFillImages[i];
                if (fill == null)
                {
                    continue;
                }

                fill.fillAmount = 0f;
                fill.gameObject.SetActive(false);
            }
        }

        uiManager?.ClearHoldProgress();
    }

    private void BuildCustomUI()
    {
        if (!useCustomSimpleUI)
        {
            return;
        }

        if (targetPoses == null)
        {
            targetPoses = new PoseData[0];
        }

        if (customPoseImageSlots == null)
        {
            customPoseImageSlots = new Image[0];
        }

        if (customHoldFillImages == null)
        {
            customHoldFillImages = new Image[0];
        }

        bool sameSlotAndFillSize = customPoseImageSlots.Length == customHoldFillImages.Length;
        bool sameAsPoseCount = customPoseImageSlots.Length == targetPoses.Length;
        if (!sameSlotAndFillSize || !sameAsPoseCount)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: customPoseImageSlots({customPoseImageSlots.Length}), customHoldFillImages({customHoldFillImages.Length}) y targetPoses({targetPoses.Length}) deben tener el mismo tamaño.", this);
            }
            return;
        }

        for (int i = 0; i < targetPoses.Length; i++)
        {
            Image poseSlot = customPoseImageSlots[i];
            if (poseSlot != null)
            {
                Sprite poseSprite = targetPoses[i] != null ? targetPoses[i].poseImage : null;
                if (poseSprite != null)
                {
                    poseSlot.sprite = poseSprite;
                    poseSlot.enabled = true;
                }
                else if (!keepSlotSpriteIfPoseSpriteMissing)
                {
                    poseSlot.sprite = null;
                    poseSlot.enabled = false;
                }
                else
                {
                    poseSlot.enabled = true;
                }

                if (debugLogs)
                {
                    Debug.Log($"{nameof(SimplePoseListener)}: UI slot[{i}] pose={(targetPoses[i] == null ? "null" : targetPoses[i].poseName)} sprite={(poseSlot.sprite == null ? "null" : poseSlot.sprite.name)} enabled={poseSlot.enabled}", this);
                }
            }
            else if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: customPoseImageSlots[{i}] es null.", this);
            }

            Image fill = customHoldFillImages[i];
            if (fill != null)
            {
                fill.fillAmount = 0f;
                fill.gameObject.SetActive(false);
            }
            else if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SimplePoseListener)}: customHoldFillImages[{i}] es null.", this);
            }
        }
    }

    private bool IsCustomUIReady()
    {
        if (!useCustomSimpleUI || customPoseImageSlots == null || customHoldFillImages == null || targetPoses == null)
        {
            return false;
        }

        return customPoseImageSlots.Length == customHoldFillImages.Length &&
               customPoseImageSlots.Length == targetPoses.Length &&
               customPoseImageSlots.Length > 0;
    }

    private void ApplyColor(Color color)
    {
        onColorChangeRequested?.Invoke(color);

        if (targetAnnotation != null)
        {
            targetAnnotation.SendMessage("SetConnectionColor", color, SendMessageOptions.DontRequireReceiver);
        }
    }
}

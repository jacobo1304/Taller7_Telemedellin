using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using mptcc = Mediapipe.Tasks.Components.Containers;

public class TwoPoseListener : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CustomPoseDetector customPoseDetector;
    [SerializeField] private SimplePoseListener simplePoseListener;
    [SerializeField] private MonoBehaviour targetAnnotation;

    [Header("Pose Targets (exactamente 2)")]
    [SerializeField] private PoseData poseOption0;
    [SerializeField] private PoseData poseOption1;

    [Header("UI (exactamente 2)")]
    [SerializeField] private Image poseImageSlot0;
    [SerializeField] private Image poseImageSlot1;
    [SerializeField] private Image fillImage0;
    [SerializeField] private Image fillImage1;
    [SerializeField] private bool keepSlotSpriteIfPoseSpriteMissing = true;

    [Header("Detection")]
    [SerializeField] private bool use3DAngles = true;
    [SerializeField, Min(0f)] private float holdTimeSeconds = 1.0f;
    [SerializeField, Min(0f)] private float defaultMarginDegrees = 20f;

    [Header("Joint Color")]
    [SerializeField] private Color defaultConnectionColor = Color.white;
    [SerializeField] private Color matchConnectionColor = Color.green;

    [Header("Flow")]
    [SerializeField] private bool autoStopOnSuccess = true;
    [SerializeField] private bool lockCustomDetectorWhileListening = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onListeningStarted;
    [SerializeField] private UnityEvent onListeningStopped;
    [SerializeField] private UnityEvent onPoseDetected;
    [SerializeField] private UnityEvent onPoseLost;
    [SerializeField] private UnityEvent onSelectPose0;
    [SerializeField] private UnityEvent onSelectPose1;

    [System.Serializable]
    public class ColorEvent : UnityEvent<Color> { }
    [SerializeField] private ColorEvent onColorChangeRequested;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugPoseScores = false;
    [SerializeField] private bool debugAngleDetails = false;
    [SerializeField, Min(0.1f)] private float debugLogInterval = 0.5f;

    private readonly object pendingFrameLock = new object();
    private List<Vector3> pendingLandmarks;
    private bool pendingFrameAvailable;
    private bool pendingFrameValid;

    private float hold0;
    private float hold1;
    private bool isListening;
    private bool completed;
    private bool anyPoseMatched;
    private bool warnedNoFeed;
    private float startedAtTime;
    private float nextDebugLogTime;
    private bool externallyPaused;

    public bool IsListening => isListening;
    public bool IsCompleted => completed;
    public bool IsExternallyPaused => externallyPaused;

    private void Awake()
    {
        startedAtTime = Time.time;

        if (customPoseDetector == null)
        {
            customPoseDetector = FindFirstObjectByType<CustomPoseDetector>();
        }

        if (simplePoseListener == null)
        {
            simplePoseListener = FindFirstObjectByType<SimplePoseListener>();
        }

        BuildTwoPoseUI();
        ClearFillUI();
        ApplyColor(defaultConnectionColor);
    }

    private void OnDisable()
    {
        StopListening(clearUI: true, unlockDetector: true, invokeEvent: false);
    }

    public void AskForTwoPoseInteraction()
    {
        if (completed)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(TwoPoseListener)}: Ya completado. Llama ResetListener().", this);
            }
            return;
        }

        if (poseOption0 == null || poseOption1 == null)
        {
            Debug.LogWarning($"{nameof(TwoPoseListener)}: Asigna poseOption0 y poseOption1.", this);
            return;
        }

        isListening = true;
        externallyPaused = false;
        anyPoseMatched = false;
        warnedNoFeed = false;
        hold0 = 0f;
        hold1 = 0f;

        BuildTwoPoseUI();
        ClearFillUI();
        ApplyColor(defaultConnectionColor);

        if (lockCustomDetectorWhileListening)
        {
            customPoseDetector?.SetResponseLock(true);
        }

        simplePoseListener?.SetExternallyPaused(true);

        onListeningStarted?.Invoke();

        if (debugLogs)
        {
            Debug.Log($"{nameof(TwoPoseListener)}: Listening started.", this);
        }
    }

    public void StopListening()
    {
        StopListening(clearUI: true, unlockDetector: true, invokeEvent: true);
    }

    public void ResetListener()
    {
        completed = false;
        StopListening(clearUI: true, unlockDetector: true, invokeEvent: false);
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

            hold0 = 0f;
            hold1 = 0f;
            ClearFillUI();
            ApplyColor(defaultConnectionColor);
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(TwoPoseListener)}: externallyPaused={(externallyPaused ? "TRUE" : "FALSE")}", this);
        }
    }

    public void ProcessLandmarks(mptcc.NormalizedLandmarks landmarks)
    {
        if (landmarks.landmarks == null || landmarks.landmarks.Count < 33)
        {
            warnedNoFeed = true;
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

    public void ProcessLandmarksList(IReadOnlyList<mptcc.NormalizedLandmarks> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            warnedNoFeed = true;
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
            ResetActiveMatch();
            return;
        }

        Evaluate(frameCopy);
    }

    private void Evaluate(IReadOnlyList<Vector3> poseData)
    {
        if (warnedNoFeed && (debugLogs || debugPoseScores || debugAngleDetails) && (Time.time - startedAtTime) > 2f && Time.time >= nextDebugLogTime)
        {
            Debug.LogWarning($"{nameof(TwoPoseListener)}: Callback con 0 targets o feed intermitente.", this);
            warnedNoFeed = false;
            nextDebugLogTime = Time.time + debugLogInterval;
        }

        bool match0 = MatchPose(poseOption0, poseData, out float avg0, out string detail0, out string score0);
        bool match1 = MatchPose(poseOption1, poseData, out float avg1, out string detail1, out string score1);

        if (debugPoseScores && Time.time >= nextDebugLogTime)
        {
            Debug.Log($"{nameof(TwoPoseListener)} Score[0] match={match0} hold={hold0:F2}/{holdTimeSeconds:F2} avgΔ={avg0:F1} :: {score0}", this);
            Debug.Log($"{nameof(TwoPoseListener)} Score[1] match={match1} hold={hold1:F2}/{holdTimeSeconds:F2} avgΔ={avg1:F1} :: {score1}", this);
        }

        if (!match0) hold0 = 0f;
        if (!match1) hold1 = 0f;

        int activeIndex = -1;
        float activeProgress = 0f;

        if (match0)
        {
            hold0 += Time.deltaTime;
            float p = holdTimeSeconds <= 0f ? 1f : Mathf.Clamp01(hold0 / holdTimeSeconds);
            if (p > activeProgress)
            {
                activeProgress = p;
                activeIndex = 0;
            }
        }

        if (match1)
        {
            hold1 += Time.deltaTime;
            float p = holdTimeSeconds <= 0f ? 1f : Mathf.Clamp01(hold1 / holdTimeSeconds);
            if (p > activeProgress)
            {
                activeProgress = p;
                activeIndex = 1;
            }
        }

        bool anyMatchNow = match0 || match1;

        if (anyMatchNow)
        {
            if (!anyPoseMatched)
            {
                anyPoseMatched = true;
                onPoseDetected?.Invoke();
                ApplyColor(matchConnectionColor);
            }

            SetFill(activeIndex, activeProgress);

            if (hold0 >= holdTimeSeconds)
            {
                CompleteSelection(0);
                return;
            }

            if (hold1 >= holdTimeSeconds)
            {
                CompleteSelection(1);
                return;
            }
        }
        else
        {
            if (anyPoseMatched)
            {
                anyPoseMatched = false;
                onPoseLost?.Invoke();
                ApplyColor(defaultConnectionColor);
            }

            ClearFillUI();

            if ((debugLogs || debugAngleDetails) && Time.time >= nextDebugLogTime)
            {
                string closest = avg0 <= avg1 ? $"0 ({detail0})" : $"1 ({detail1})";
                Debug.Log($"{nameof(TwoPoseListener)}: no pose matched. Closest={closest}", this);
            }
        }

        if ((debugLogs || debugAngleDetails || debugPoseScores) && Time.time >= nextDebugLogTime)
        {
            if (anyMatchNow)
            {
                Debug.Log($"{nameof(TwoPoseListener)}: candidate matched. activeIndex={activeIndex}, progress={activeProgress:F2}", this);
            }
            nextDebugLogTime = Time.time + debugLogInterval;
        }
    }

    private bool MatchPose(PoseData pose, IReadOnlyList<Vector3> poseData, out float avgDelta, out string failDetail, out string scoreDetail)
    {
        avgDelta = float.MaxValue;
        failDetail = string.Empty;
        scoreDetail = string.Empty;

        if (pose == null || pose.conditions == null || pose.conditions.Count == 0)
        {
            failDetail = "pose/conditions null";
            return false;
        }

        float margin = pose.marginDegrees > 0f ? pose.marginDegrees : defaultMarginDegrees;
        float totalDelta = 0f;
        int count = 0;

        for (int i = 0; i < pose.conditions.Count; i++)
        {
            var cond = pose.conditions[i];
            cond.GetIndices(out int iA, out int iB, out int iC);

            if (iA < 0 || iA >= poseData.Count || iB < 0 || iB >= poseData.Count || iC < 0 || iC >= poseData.Count)
            {
                failDetail = $"Índices inválidos ({iA},{iB},{iC}) Count={poseData.Count}";
                return false;
            }

            float angle = CalculateAngle(poseData[iA], poseData[iB], poseData[iC]);
            float delta = Mathf.Abs(angle - cond.targetAngle);
            totalDelta += delta;
            count++;
            scoreDetail += $"[{cond.jointType}: {angle:F1}/{cond.targetAngle:F1} Δ{delta:F1}] ";

            if (delta > margin)
            {
                failDetail = $"{cond.jointType}: angle={angle:F1}, target={cond.targetAngle:F1}, delta={delta:F1}, margin={margin:F1}";
                avgDelta = count > 0 ? totalDelta / count : float.MaxValue;
                return false;
            }
        }

        avgDelta = count > 0 ? totalDelta / count : float.MaxValue;
        failDetail = "all conditions within margin";
        return true;
    }

    private void CompleteSelection(int selectedIndex)
    {
        completed = true;
        SetFill(selectedIndex, 1f);

        if (selectedIndex == 0)
        {
            onSelectPose0?.Invoke();
        }
        else
        {
            onSelectPose1?.Invoke();
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(TwoPoseListener)}: Hold complete -> selectedIndex={selectedIndex}", this);
        }

        if (autoStopOnSuccess)
        {
            StopListening(clearUI: false, unlockDetector: true, invokeEvent: false);
        }
    }

    private void StopListening(bool clearUI, bool unlockDetector, bool invokeEvent)
    {
        bool wasListening = isListening;
        isListening = false;

        lock (pendingFrameLock)
        {
            pendingFrameAvailable = false;
        }

        hold0 = 0f;
        hold1 = 0f;

        if (clearUI)
        {
            ClearFillUI();
        }

        if (anyPoseMatched)
        {
            anyPoseMatched = false;
            onPoseLost?.Invoke();
        }

        ApplyColor(defaultConnectionColor);

        if (unlockDetector && lockCustomDetectorWhileListening)
        {
            customPoseDetector?.SetResponseLock(false);
        }

        simplePoseListener?.SetExternallyPaused(false);

        if (wasListening && invokeEvent)
        {
            onListeningStopped?.Invoke();
        }
    }

    private void ResetActiveMatch()
    {
        hold0 = 0f;
        hold1 = 0f;
        ClearFillUI();

        if (anyPoseMatched)
        {
            anyPoseMatched = false;
            onPoseLost?.Invoke();
            ApplyColor(defaultConnectionColor);
        }
    }

    private void BuildTwoPoseUI()
    {
        ApplyPoseSprite(poseImageSlot0, poseOption0);
        ApplyPoseSprite(poseImageSlot1, poseOption1);
    }

    private void ApplyPoseSprite(Image slot, PoseData pose)
    {
        if (slot == null)
        {
            return;
        }

        Sprite sprite = pose != null ? pose.poseImage : null;
        if (sprite != null)
        {
            slot.sprite = sprite;
            slot.enabled = true;
        }
        else if (!keepSlotSpriteIfPoseSpriteMissing)
        {
            slot.sprite = null;
            slot.enabled = false;
        }
        else
        {
            slot.enabled = true;
        }
    }

    private void SetFill(int activeIndex, float normalizedProgress)
    {
        float p = Mathf.Clamp01(normalizedProgress);

        if (fillImage0 != null)
        {
            bool active = activeIndex == 0;
            fillImage0.gameObject.SetActive(active && p > 0f);
            fillImage0.fillAmount = active ? p : 0f;
        }

        if (fillImage1 != null)
        {
            bool active = activeIndex == 1;
            fillImage1.gameObject.SetActive(active && p > 0f);
            fillImage1.fillAmount = active ? p : 0f;
        }
    }

    private void ClearFillUI()
    {
        if (fillImage0 != null)
        {
            fillImage0.fillAmount = 0f;
            fillImage0.gameObject.SetActive(false);
        }

        if (fillImage1 != null)
        {
            fillImage1.fillAmount = 0f;
            fillImage1.gameObject.SetActive(false);
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

    private void ApplyColor(Color color)
    {
        onColorChangeRequested?.Invoke(color);

        if (targetAnnotation != null)
        {
            targetAnnotation.SendMessage("SetConnectionColor", color, SendMessageOptions.DontRequireReceiver);
        }
    }
}

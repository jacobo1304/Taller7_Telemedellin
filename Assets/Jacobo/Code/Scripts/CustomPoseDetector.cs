using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using mptcc = Mediapipe.Tasks.Components.Containers;

public class CustomPoseDetector : MonoBehaviour
{
    [System.Serializable]
    public class CustomPoseConfig
    {
        public string poseName = "New Pose";
        [Tooltip("Question ID to send to AnswerHandler when pose is matched")]
        public string questionId;
        public bool answerValue = true;

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
    [Tooltip("Target annotation script to change connection colors (e.g. PoseLandmarkListAnnotation or MultiPoseLandmarkListWithMaskAnnotation)")]
    public MonoBehaviour targetAnnotation;

    [Header("Settings")]
    public Color defaultConnectionColor = Color.white;
    public Color matchConnectionColor = Color.green;

    [Header("Poses")]
    public List<CustomPoseConfig> customPoses = new List<CustomPoseConfig>();

    [System.Serializable]
    public class ColorEvent : UnityEvent<Color> { }

    [Header("Optional Output Events")]
    public UnityEvent onAnyPoseMatched;
    public UnityEvent onAllPosesLost;
    public ColorEvent onColorChangeRequested; // Connect this to SetConnectionColor externally if needed

    private bool anyWasMatched = false;

    private void Start()
    {
        if (answerHandler == null)
            answerHandler = FindFirstObjectByType<AnswerHandler>();

        ApplyColor(defaultConnectionColor);
    }

    // Call this from MediaPipe tasks, e.g. PoseDetection graph
    public void ProcessLandmarks(mptcc.NormalizedLandmarks landmarks)
    {
        if (landmarks.landmarks == null || landmarks.landmarks.Count < 33)
        {
            ResetAllHolds();
            return;
        }

        EvaluatePoses(landmarks);
    }

    // Overload for multiple targets
    public void ProcessLandmarksList(IReadOnlyList<mptcc.NormalizedLandmarks> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            ResetAllHolds();
            return;
        }

        // Just evaluate the first detected person
        ProcessLandmarks(targets[0]);
    }

    private void EvaluatePoses(mptcc.NormalizedLandmarks poseData)
    {
        bool currentAnyMatched = false;

        foreach (var pose in customPoses)
        {
            bool match = true;

            foreach (var cond in pose.conditions)
            {
                cond.GetIndices(out int iA, out int iB, out int iC);
                
                var lA = poseData.landmarks[iA];
                var lB = poseData.landmarks[iB];
                var lC = poseData.landmarks[iC];

                float angle = CalculateAngle(lA, lB, lC);

                if (Mathf.Abs(angle - cond.targetAngle) > pose.marginDegrees)
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                currentAnyMatched = true;
                pose.isMatched = true;
                pose.currentHoldTimer += Time.deltaTime;

                if (pose.currentHoldTimer >= pose.holdTime && !pose.eventFired)
                {
                    pose.eventFired = true;
                    // Send to Answer Handler
                    if (answerHandler != null && !string.IsNullOrEmpty(pose.questionId))
                    {
                        answerHandler.SubmitAnswer(pose.questionId, pose.answerValue);
                        Debug.Log($"Pose '{pose.poseName}' detected! Sent answer for {pose.questionId}");
                    }
                }
            }
            else
            {
                pose.isMatched = false;
                pose.currentHoldTimer = 0f;
                pose.eventFired = false;
            }
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

        if (anyWasMatched)
        {
            ApplyColor(defaultConnectionColor);
            onAllPosesLost?.Invoke();
            anyWasMatched = false;
        }
    }

    private float CalculateAngle(mptcc.NormalizedLandmark a, mptcc.NormalizedLandmark b, mptcc.NormalizedLandmark c)
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
}

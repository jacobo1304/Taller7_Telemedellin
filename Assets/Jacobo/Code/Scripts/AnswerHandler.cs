using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

public class AnswerHandler : MonoBehaviour
{
    public enum EnvironmentActionType
    {
        None,
        TiltMainCamera,
        SetLightsEnabled,
        SetAudioSourcePlaying
    }

    [Serializable]
    public class AnswerEvent : UnityEvent<string> { }

    [Serializable]
    public class QuestionActionConfig
    {
        [Header("Question")]
        public string questionId;

        [Header("Actions by Result")]
        public EnvironmentActionType correctAction = EnvironmentActionType.None;
        public EnvironmentActionType wrongAction = EnvironmentActionType.None;

        [Header("Camera Tilt")]
        [Tooltip("If empty, Camera.main will be used.")]
        public Camera targetCamera;
        [Tooltip("Rotation offset in Euler degrees.")]
        public Vector3 tiltEulerOffset = new Vector3(0f, 0f, 8f);
        [Min(0f)] public float tiltDuration = 0.35f;
        public bool returnAfterTilt = false;
        [Min(0f)] public float returnDuration = 0.35f;

        [Header("Lights")]
        public Light[] targetLights;
        public bool lightsEnabled = false;

        [Header("Audio")]
        public AudioSource targetAudioSource;
        public bool audioShouldPlay = true;

        [Header("Optional Extra Events")]
        public UnityEvent onCorrect;
        public UnityEvent onWrong;
    }

    [Header("Question Configuration")]
    [SerializeField] private List<QuestionActionConfig> questionConfigs = new List<QuestionActionConfig>();

    [Header("Global Events")]
    public AnswerEvent onAnswerCorrect;
    public AnswerEvent onAnswerWrong;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly Dictionary<string, QuestionActionConfig> configByQuestionId = new Dictionary<string, QuestionActionConfig>();
    private Coroutine cameraTiltRoutine;

    private void Awake()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        configByQuestionId.Clear();

        for (int i = 0; i < questionConfigs.Count; i++)
        {
            QuestionActionConfig config = questionConfigs[i];

            if (config == null || string.IsNullOrWhiteSpace(config.questionId))
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"{nameof(AnswerHandler)}: Empty question config at index {i}.", this);
                }
                continue;
            }

            string key = config.questionId.Trim();
            if (configByQuestionId.ContainsKey(key))
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"{nameof(AnswerHandler)}: Duplicate questionId '{key}'. Keeping first one.", this);
                }
                continue;
            }

            configByQuestionId.Add(key, config);
        }
    }

    public void SubmitAnswer(string questionId, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(questionId))
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AnswerHandler)}: Received empty questionId.", this);
            }
            return;
        }

        string key = questionId.Trim();

        if (isCorrect)
        {
            onAnswerCorrect?.Invoke(key);
        }
        else
        {
            onAnswerWrong?.Invoke(key);
        }

        if (!configByQuestionId.TryGetValue(key, out QuestionActionConfig config))
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AnswerHandler)}: No config found for questionId '{key}'.", this);
            }
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(AnswerHandler)}: Question '{key}' answered {(isCorrect ? "CORRECT" : "WRONG")}", this);
        }

        ExecuteAction(config, isCorrect);

        if (isCorrect)
        {
            config.onCorrect?.Invoke();
        }
        else
        {
            config.onWrong?.Invoke();
        }
    }

    private void ExecuteAction(QuestionActionConfig config, bool isCorrect)
    {
        EnvironmentActionType action = isCorrect ? config.correctAction : config.wrongAction;

        switch (action)
        {
            case EnvironmentActionType.None:
                return;

            case EnvironmentActionType.TiltMainCamera:
                Camera cameraToTilt = config.targetCamera != null ? config.targetCamera : Camera.main;
                if (cameraToTilt == null)
                {
                    if (debugLogs)
                    {
                        Debug.LogWarning($"{nameof(AnswerHandler)}: Tilt action requested but no camera was assigned and no Camera.main exists.", this);
                    }
                    return;
                }

                if (cameraTiltRoutine != null)
                {
                    StopCoroutine(cameraTiltRoutine);
                }

                cameraTiltRoutine = StartCoroutine(TiltCameraRoutine(
                    cameraToTilt,
                    config.tiltEulerOffset,
                    config.tiltDuration,
                    config.returnAfterTilt,
                    config.returnDuration));
                return;

            case EnvironmentActionType.SetLightsEnabled:
                if (config.targetLights == null || config.targetLights.Length == 0)
                {
                    if (debugLogs)
                    {
                        Debug.LogWarning($"{nameof(AnswerHandler)}: Light action requested but no lights were assigned for question '{config.questionId}'.", this);
                    }
                    return;
                }

                for (int i = 0; i < config.targetLights.Length; i++)
                {
                    Light lightTarget = config.targetLights[i];
                    if (lightTarget != null)
                    {
                        lightTarget.enabled = config.lightsEnabled;
                    }
                }
                return;

            case EnvironmentActionType.SetAudioSourcePlaying:
                if (config.targetAudioSource == null)
                {
                    if (debugLogs)
                    {
                        Debug.LogWarning($"{nameof(AnswerHandler)}: Audio action requested but no AudioSource was assigned for question '{config.questionId}'.", this);
                    }
                    return;
                }

                if (config.audioShouldPlay)
                {
                    if (!config.targetAudioSource.isPlaying)
                    {
                        config.targetAudioSource.Play();
                    }
                }
                else
                {
                    config.targetAudioSource.Stop();
                }
                return;
        }
    }

    private IEnumerator TiltCameraRoutine(Camera targetCamera, Vector3 eulerOffset, float tiltDuration, bool returnAfterTilt, float returnDuration)
    {
        Transform cameraTransform = targetCamera.transform;
        Quaternion initialRotation = cameraTransform.rotation;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(eulerOffset);

        if (tiltDuration <= 0f)
        {
            cameraTransform.rotation = targetRotation;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < tiltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tiltDuration);
                cameraTransform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
                yield return null;
            }
        }

        if (!returnAfterTilt)
        {
            cameraTiltRoutine = null;
            yield break;
        }

        if (returnDuration <= 0f)
        {
            cameraTransform.rotation = initialRotation;
            cameraTiltRoutine = null;
            yield break;
        }

        float returnElapsed = 0f;
        while (returnElapsed < returnDuration)
        {
            returnElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(returnElapsed / returnDuration);
            cameraTransform.rotation = Quaternion.Slerp(targetRotation, initialRotation, t);
            yield return null;
        }

        cameraTransform.rotation = initialRotation;
        cameraTiltRoutine = null;
    }
}

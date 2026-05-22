using UnityEngine;
using UnityEngine.UI;

public class AudioMeterUI : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI")]
    [SerializeField] private Image levelFillImage;
    [SerializeField] private Image clipIndicatorImage;

    [Header("Meter Settings")]
    [Tooltip("Minimum dB shown as empty (e.g., -60).")]
    [SerializeField] private float minDb = -60f;
    [Tooltip("Maximum dB shown as full (e.g., 0).")]
    [SerializeField] private float maxDb = 0f;
    [Tooltip("Level that triggers clip indicator in dB.")]
    [SerializeField] private float clipThresholdDb = -3f;
    [Tooltip("Sample amplitude threshold to consider as clipped (0-1).")]
    [SerializeField] private float clipSampleThreshold = 0.98f;
    [Tooltip("How fast the meter rises.")]
    [SerializeField] private float riseSpeed = 20f;
    [Tooltip("How fast the meter falls.")]
    [SerializeField] private float fallSpeed = 8f;
    [Tooltip("Keep clip indicator active for this time after clipping.")]
    [SerializeField] private float clipHoldTime = 0.2f;
    [Tooltip("Treat enabled AudioDistortionFilter as clipping.")]
    [SerializeField] private bool distortionTriggersClip = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float[] samples = new float[256];
    private float currentFill = 0f;
    private float clipTimer = 0f;

    public void SetAudioSource(AudioSource newSource)
    {
        audioSource = newSource;
    }

    private void Reset()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Awake()
    {
        if (clipIndicatorImage != null)
        {
            clipIndicatorImage.enabled = false;
        }
    }

    private void Update()
    {
        if (audioSource == null || levelFillImage == null)
        {
            return;
        }

        float rms = GetRmsLevel();
        float db = RmsToDb(rms);
        float normalized = Mathf.InverseLerp(minDb, maxDb, db);

        float speed = normalized > currentFill ? riseSpeed : fallSpeed;
        currentFill = Mathf.MoveTowards(currentFill, normalized, speed * Time.deltaTime);
        levelFillImage.fillAmount = Mathf.Clamp01(currentFill);

        bool hasDistortion = distortionTriggersClip && IsDistortionEnabled();
        bool isClipping = hasDistortion || db >= clipThresholdDb || HasClippedSample();
        if (isClipping)
        {
            clipTimer = clipHoldTime;
        }
        else
        {
            clipTimer = Mathf.Max(0f, clipTimer - Time.deltaTime);
        }

        if (clipIndicatorImage != null)
        {
            clipIndicatorImage.enabled = clipTimer > 0f;
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(AudioMeterUI)}: rms={rms:F4}, db={db:F2}, fill={currentFill:F2}, clip={isClipping}");
        }
    }

    private float GetRmsLevel()
    {
        audioSource.GetOutputData(samples, 0);
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float sample = samples[i];
            sum += sample * sample;
        }

        float rms = Mathf.Sqrt(sum / samples.Length);
        return rms;
    }

    private bool HasClippedSample()
    {
        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) >= clipSampleThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDistortionEnabled()
    {
        if (audioSource == null)
        {
            return false;
        }

        AudioDistortionFilter filter = audioSource.GetComponent<AudioDistortionFilter>();
        return filter != null && filter.enabled;
    }

    private static float RmsToDb(float rms)
    {
        if (rms <= 0f)
        {
            return -80f;
        }

        return 20f * Mathf.Log10(rms);
    }
}

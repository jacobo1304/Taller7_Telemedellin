using System;
using UnityEngine;

public class TimerFeedbackController : MonoBehaviour
{
    [Serializable]
    public class TimeAudioCue
    {
        [Tooltip("Seconds remaining to trigger this cue.")]
        public float secondsRemaining = 10f;
        public AudioClip[] clips = new AudioClip[0];
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Question Music")]
    [SerializeField] private AudioSource questionMusicSource;
    [SerializeField] private float minPitch = 1f;
    [SerializeField] private float maxPitch = 1.5f;

    [Header("Clock Animation")]
    [SerializeField] private Animator clockAnimator;
    [SerializeField] private string clockTriggerName = "ClockStart";
    [SerializeField] private float clockTriggerSeconds = 10f;

    [Header("Time Audio Cues")]
    [SerializeField] private AudioSource cueAudioSource;
    [SerializeField] private TimeAudioCue[] cues = new TimeAudioCue[0];

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool clockTriggered = false;
    private bool[] firedCues = new bool[0];

    public void OnTimerStart(float startTime)
    {
        clockTriggered = false;
        PrepareCueState();
        UpdatePitch(startTime, startTime);
    }

    public void OnTick(float remaining, float startTime)
    {
        UpdatePitch(remaining, startTime);
        TryTriggerClock(remaining);
        TryTriggerCues(remaining);
    }

    public void OnTimeout()
    {
        UpdatePitch(0f, 1f);
    }

    public void OnTimerStopped()
    {
        UpdatePitch(1f, 1f);
    }

    private void PrepareCueState()
    {
        if (cues == null)
        {
            firedCues = new bool[0];
            return;
        }

        firedCues = new bool[cues.Length];
    }

    private void UpdatePitch(float remaining, float startTime)
    {
        if (questionMusicSource == null)
        {
            return;
        }

        float safeStart = Mathf.Max(0.01f, startTime);
        float t = Mathf.Clamp01(1f - Mathf.Clamp01(remaining / safeStart));
        float pitch = Mathf.Lerp(minPitch, maxPitch, t);
        questionMusicSource.pitch = pitch;
    }

    private void TryTriggerClock(float remaining)
    {
        if (clockTriggered || clockAnimator == null || string.IsNullOrWhiteSpace(clockTriggerName))
        {
            return;
        }

        if (remaining <= clockTriggerSeconds)
        {
            clockAnimator.SetTrigger(clockTriggerName);
            clockTriggered = true;

            if (debugLogs)
            {
                Debug.Log($"{nameof(TimerFeedbackController)}: Clock trigger fired at {remaining:F2}s.");
            }
        }
    }

    private void TryTriggerCues(float remaining)
    {
        if (cues == null || cues.Length == 0 || cueAudioSource == null)
        {
            return;
        }

        if (firedCues == null || firedCues.Length != cues.Length)
        {
            firedCues = new bool[cues.Length];
        }

        for (int i = 0; i < cues.Length; i++)
        {
            if (firedCues[i])
            {
                continue;
            }

            TimeAudioCue cue = cues[i];
            if (cue == null)
            {
                firedCues[i] = true;
                continue;
            }

            if (remaining <= cue.secondsRemaining)
            {
                PlayCue(cue);
                firedCues[i] = true;
            }
        }
    }

    private void PlayCue(TimeAudioCue cue)
    {
        if (cueAudioSource == null || cue.clips == null || cue.clips.Length == 0)
        {
            return;
        }

        AudioClip clip = cue.clips[UnityEngine.Random.Range(0, cue.clips.Length)];
        if (clip == null)
        {
            return;
        }

        cueAudioSource.PlayOneShot(clip, cue.volume);

        if (debugLogs)
        {
            Debug.Log($"{nameof(TimerFeedbackController)}: Playing cue '{clip.name}' at {cue.secondsRemaining:F2}s.");
        }
    }
}

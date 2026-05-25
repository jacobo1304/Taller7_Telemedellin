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
    [Tooltip("Clip fijo de música que debe sonar durante la pregunta (su duración debe coincidir con el tiempo de la pregunta).")]
    [SerializeField] private AudioClip questionMusicClip;
    [Tooltip("Volumen para la música de la pregunta (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float questionMusicVolume = 1f;

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

    private InteractionType currentInteractionType = InteractionType.Titulares;
    private bool hasCurrentInteractionType;

    private void Awake()
    {
        if (questionMusicSource == null)
        {
            questionMusicSource = GetComponent<AudioSource>();
        }
    }

    public void SetCurrentInteractionType(InteractionType interactionType)
    {
        currentInteractionType = interactionType;
        hasCurrentInteractionType = true;

        if (debugLogs)
        {
            Debug.Log($"{nameof(TimerFeedbackController)}: CurrentInteractionType={currentInteractionType}", this);
        }
    }

    public void OnTimerStart(float startTime)
    {
        clockTriggered = false;
        PrepareCueState();

        // Reset any previous audio.
        StopQuestionMusic();

        if (cueAudioSource != null)
        {
            cueAudioSource.Stop();
        }

        TryStartQuestionMusic();

        if (debugLogs)
        {
            Debug.Log($"{nameof(TimerFeedbackController)}: Timer started. startTime={startTime:F2}s interaction={(hasCurrentInteractionType ? currentInteractionType.ToString() : "(not-set)")}", this);
        }
    }

    public void OnTick(float remaining, float startTime)
    {
        TryTriggerClock(remaining);
        TryTriggerCues(remaining);
    }

    public void OnTimeout()
    {
        StopQuestionMusic();

        if (debugLogs)
        {
            Debug.Log($"{nameof(TimerFeedbackController)}: Timeout.", this);
        }
    }

    public void OnTimerStopped()
    {
        StopQuestionMusic();

        if (cueAudioSource != null)
        {
            cueAudioSource.Stop();
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(TimerFeedbackController)}: Timer stopped.", this);
        }
    }

    public void StopQuestionMusic()
    {
        if (questionMusicSource == null)
        {
            return;
        }

        if (questionMusicSource.isPlaying)
        {
            questionMusicSource.Stop();
        }
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
        // En la interacción de Sonidos NO reproducimos cues durante la pregunta.
        if (currentInteractionType == InteractionType.Sonidos)
        {
            return;
        }

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

    private void TryStartQuestionMusic()
    {
        // En la interacción de Sonidos NO suena la música de la pregunta.
        if (currentInteractionType == InteractionType.Sonidos)
        {
            return;
        }

        if (questionMusicSource == null || questionMusicClip == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(TimerFeedbackController)}: Missing questionMusicSource or questionMusicClip.", this);
            }
            return;
        }

        questionMusicSource.playOnAwake = false;
        questionMusicSource.loop = false;
        questionMusicSource.volume = questionMusicVolume;
        questionMusicSource.clip = questionMusicClip;
        questionMusicSource.time = 0f;
        questionMusicSource.Play();

        if (debugLogs)
        {
            Debug.Log($"{nameof(TimerFeedbackController)}: Playing question music '{questionMusicClip.name}'.", this);
        }
    }
}

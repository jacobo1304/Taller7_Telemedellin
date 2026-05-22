using UnityEngine;
using System.Collections;

public class SoundCrewInteractionAction : InteractionActionBase
{
    [Header("Faders Knobs")]
    [SerializeField] private Transform ambienceKnob;
    [SerializeField] private Transform voiceKnob;

    [Header("Knob Position Targets por opción")]
    [SerializeField] private Transform[] ambienceKnobOptionSpots = new Transform[3];
    [SerializeField] private Transform[] voiceKnobOptionSpots = new Transform[3];

    [Header("AudioSources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource ambienceAudioSource;

    [Header("UI")]
    [SerializeField] private AudioMeterUI audioMeterUI;

    [System.Serializable]
    private class AudioOptionProfile
    {
        [Range(0f, 1f)] public float voiceVolume = 1f;
        [Range(0f, 1f)] public float ambienceVolume = 0.2f;

        public bool voiceDistortion = false;
        public bool ambienceDistortion = false;
    }

    [Header("Perfiles por opción")]
    [SerializeField] private AudioOptionProfile[] optionProfiles = new AudioOptionProfile[3]
    {
        new AudioOptionProfile(),
        new AudioOptionProfile(),
        new AudioOptionProfile()
    };

    [Header("Interpolación")]
    [SerializeField] private float knobLerpDuration = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine transitionRoutine;
    private int currentPreviewOption = -1;
    private AudioOptionProfile currentProfile;

    public AudioSource VoiceAudioSource => voiceAudioSource;

    public void StartPlayback()
    {
        StartAudioSource(voiceAudioSource);
        StartAudioSource(ambienceAudioSource);

        if (audioMeterUI != null && voiceAudioSource != null)
        {
            audioMeterUI.SetAudioSource(voiceAudioSource);
        }
    }

    public void StopPlayback()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();

        if (ambienceAudioSource != null)
            ambienceAudioSource.Stop();
    }

    public void SetClipsAndRestart(
        AudioClip voiceClip,
        AudioClip ambienceClip
    )
    {
        if (voiceAudioSource != null && voiceClip != null)
        {
            voiceAudioSource.clip = voiceClip;
        }

        if (ambienceAudioSource != null && ambienceClip != null)
        {
            ambienceAudioSource.clip = ambienceClip;
        }

        StopPlayback();
        StartPlayback();
    }

    private void StartAudioSource(AudioSource source)
    {
        if (source == null || source.clip == null)
            return;

        source.playOnAwake = false;

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    public override void PreviewOption(int selectedOptionIndex)
    {
        ApplyOptionState(selectedOptionIndex);
    }

    public override void ResetHoldEffects()
    {
    }

    public override void RestoreStoredSelection()
    {
        if (!HasStoredSelection)
        {
            Debug.LogWarning("No hay selección guardada de sonido.");
            return;
        }

        StartPlayback();

        // Si el objeto está apagado, aplicar instantáneamente
        bool instantApply =
            !gameObject.activeInHierarchy ||
            !isActiveAndEnabled;

        ForceApplyOptionState(
            StoredSelectedOptionIndex,
            instantApply
        );
    }

    protected override void ApplyCorrectEffect()
    {
        ApplyOptionState(CorrectOptionIndex);
    }

    protected override void ApplyWrongEffect1()
    {
        ApplyOptionState(WrongOption1Index);
    }

    protected override void ApplyWrongEffect2()
    {
        ApplyOptionState(WrongOption2Index);
    }

    private void ApplyOptionState(int optionIndex)
    {
        ApplyOptionState(optionIndex, false, false);
    }

    private void ForceApplyOptionState(
        int optionIndex,
        bool instant = false
    )
    {
        ApplyOptionState(optionIndex, true, instant);
    }

    private void ApplyOptionState(
        int optionIndex,
        bool force,
        bool instant
    )
    {
        if (optionProfiles == null ||
            optionProfiles.Length == 0)
        {
            return;
        }

        int clampedOption =
            Mathf.Clamp(
                optionIndex,
                0,
                optionProfiles.Length - 1
            );

        if (!force &&
            clampedOption == currentPreviewOption)
        {
            return;
        }

        currentPreviewOption = clampedOption;
        currentProfile =
            optionProfiles[clampedOption];

        Transform targetAmbienceSpot =
            ambienceKnobOptionSpots != null &&
            clampedOption < ambienceKnobOptionSpots.Length
            ? ambienceKnobOptionSpots[clampedOption]
            : null;

        Transform targetVoiceSpot =
            voiceKnobOptionSpots != null &&
            clampedOption < voiceKnobOptionSpots.Length
            ? voiceKnobOptionSpots[clampedOption]
            : null;

        // Si está inactivo o queremos instantáneo
        if (instant || !gameObject.activeInHierarchy)
        {
            ApplyInstantState(
                targetAmbienceSpot,
                targetVoiceSpot
            );
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine =
            StartCoroutine(
                TransitionRoutine(
                    targetAmbienceSpot,
                    targetVoiceSpot
                )
            );
    }

    private void ApplyInstantState(
        Transform targetAmbienceSpot,
        Transform targetVoiceSpot
    )
    {
        if (ambienceKnob != null &&
            targetAmbienceSpot != null)
        {
            ambienceKnob.position =
                targetAmbienceSpot.position;
        }

        if (voiceKnob != null &&
            targetVoiceSpot != null)
        {
            voiceKnob.position =
                targetVoiceSpot.position;
        }

        ApplyProfileVolumes();

        if (debugLogs)
        {
            Debug.Log(
                "Sound aplicado instantáneamente."
            );
        }
    }

    private IEnumerator TransitionRoutine(
        Transform targetAmbienceSpot,
        Transform targetVoiceSpot
    )
    {
        Vector3 startAmbiencePos =
            ambienceKnob != null
            ? ambienceKnob.position
            : Vector3.zero;

        Vector3 startVoicePos =
            voiceKnob != null
            ? voiceKnob.position
            : Vector3.zero;

        Vector3 targetAmbiencePos =
            targetAmbienceSpot != null
            ? targetAmbienceSpot.position
            : startAmbiencePos;

        Vector3 targetVoicePos =
            targetVoiceSpot != null
            ? targetVoiceSpot.position
            : startVoicePos;

        float duration =
            Mathf.Max(0.001f, knobLerpDuration);

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float kPos =
                Mathf.Clamp01(t / duration);

            if (ambienceKnob != null)
            {
                ambienceKnob.position =
                    Vector3.Lerp(
                        startAmbiencePos,
                        targetAmbiencePos,
                        kPos
                    );
            }

            if (voiceKnob != null)
            {
                voiceKnob.position =
                    Vector3.Lerp(
                        startVoicePos,
                        targetVoicePos,
                        kPos
                    );
            }

            ApplyProfileVolumes();

            yield return null;
        }

        ApplyProfileVolumes();

        transitionRoutine = null;
    }

    private void ApplyProfileVolumes()
    {
        if (currentProfile == null)
            return;

        if (voiceAudioSource != null)
        {
            voiceAudioSource.volume =
                currentProfile.voiceVolume;

            SetDistortionState(
                voiceAudioSource,
                currentProfile.voiceDistortion
            );
        }

        if (ambienceAudioSource != null)
        {
            ambienceAudioSource.volume =
                currentProfile.ambienceVolume;

            SetDistortionState(
                ambienceAudioSource,
                currentProfile.ambienceDistortion
            );
        }
    }

    private void SetDistortionState(
        AudioSource source,
        bool enabled
    )
    {
        if (source == null)
            return;

        AudioDistortionFilter filter =
            source.GetComponent<AudioDistortionFilter>();

        if (filter != null)
        {
            filter.enabled = enabled;
        }
    }
}
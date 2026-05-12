using UnityEngine;
using System.Collections;

public class SoundCrewInteractionAction : InteractionActionBase
{
    [Header("Faders Knobs")]
    [SerializeField] private Transform ambienceKnob;
    [SerializeField] private Transform voiceKnob;

    [Header("Knob Position Targets por opción (índices 0,1,2)")]
    [Tooltip("Posiciones objetivo del knob de ambiente para cada opción")]
    [SerializeField] private Transform[] ambienceKnobOptionSpots = new Transform[3];
    [Tooltip("Posiciones objetivo del knob de voz para cada opción")]
    [SerializeField] private Transform[] voiceKnobOptionSpots = new Transform[3];

    [Header("AudioSources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource ambienceAudioSource;

    [System.Serializable]
    private class AudioOptionProfile
    {
        [Header("Volumen")]
        [Range(0f, 1f)] public float voiceVolume = 1f;
        [Range(0f, 1f)] public float ambienceVolume = 0.2f;

        [Header("Distorsión")]
        public bool voiceDistortion = false;
        public bool ambienceDistortion = false;
    }

    [Header("Perfiles por opción (índices 0,1,2)")]
    [SerializeField] private AudioOptionProfile[] optionProfiles = new AudioOptionProfile[3]
    {
        new AudioOptionProfile { voiceVolume = 1f, ambienceVolume = 0.2f, voiceDistortion = false, ambienceDistortion = false },
        new AudioOptionProfile { voiceVolume = 0.7f, ambienceVolume = 0.4f, voiceDistortion = false, ambienceDistortion = false },
        new AudioOptionProfile { voiceVolume = 0.4f, ambienceVolume = 0.8f, voiceDistortion = true, ambienceDistortion = false }
    };

    [Header("Interpolación")]
    [SerializeField] private float knobLerpDuration = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine transitionRoutine;
    private int currentPreviewOption = -1;
    private AudioOptionProfile currentProfile;

    public void StartPlayback()
    {
        StartAudioSource(voiceAudioSource);
        StartAudioSource(ambienceAudioSource);
    }
    public void StopPlayback()
    {
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }

        if (ambienceAudioSource != null && ambienceAudioSource.isPlaying)
        {
            ambienceAudioSource.Stop();
        }
    }

    private void StartAudioSource(AudioSource source)
    {
        if (source == null || source.clip == null)
        {
            return;
        }

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
        // Opcionalmente puede restablecer un estado por defecto si se requiere.
    }

    public override void RestoreStoredSelection()
    {
        StartPlayback();
        ForceApplyOptionState(StoredSelectedOptionIndex);
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
        ApplyOptionState(optionIndex, false);
    }

    private void ForceApplyOptionState(int optionIndex)
    {
        ApplyOptionState(optionIndex, true);
    }

    private void ApplyOptionState(int optionIndex, bool force)
    {
        if (optionProfiles == null || optionProfiles.Length == 0)
        {
            return;
        }

        int clampedOption = Mathf.Clamp(optionIndex, 0, optionProfiles.Length - 1);
        if (!force && clampedOption == currentPreviewOption)
        {
            return;
        }

        currentPreviewOption = clampedOption;
        currentProfile = optionProfiles[clampedOption];

        Transform targetAmbienceSpot = ambienceKnobOptionSpots != null && clampedOption < ambienceKnobOptionSpots.Length
            ? ambienceKnobOptionSpots[clampedOption]
            : null;

        Transform targetVoiceSpot = voiceKnobOptionSpots != null && clampedOption < voiceKnobOptionSpots.Length
            ? voiceKnobOptionSpots[clampedOption]
            : null;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(TransitionRoutine(targetAmbienceSpot, targetVoiceSpot));
    }

    private IEnumerator TransitionRoutine(Transform targetAmbienceSpot, Transform targetVoiceSpot)
    {
        Vector3 startAmbiencePos = ambienceKnob != null ? ambienceKnob.position : Vector3.zero;
        Vector3 startVoicePos = voiceKnob != null ? voiceKnob.position : Vector3.zero;

        Vector3 targetAmbiencePos = targetAmbienceSpot != null ? targetAmbienceSpot.position : startAmbiencePos;
        Vector3 targetVoicePos = targetVoiceSpot != null ? targetVoiceSpot.position : startVoicePos;

        float duration = Mathf.Max(0.001f, knobLerpDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float kPos = knobLerpDuration <= 0f ? 1f : Mathf.Clamp01(t / knobLerpDuration);

            if (ambienceKnob != null)
            {
                ambienceKnob.position = Vector3.Lerp(startAmbiencePos, targetAmbiencePos, kPos);
            }

            if (voiceKnob != null)
            {
                voiceKnob.position = Vector3.Lerp(startVoicePos, targetVoicePos, kPos);
            }

            ApplyProfileVolumes();
            yield return null;
        }

        if (ambienceKnob != null)
        {
            ambienceKnob.position = targetAmbiencePos;
        }

        if (voiceKnob != null)
        {
            voiceKnob.position = targetVoicePos;
        }

        ApplyProfileVolumes();
        transitionRoutine = null;
    }

    private void ApplyProfileVolumes()
    {
        if (currentProfile == null)
        {
            return;
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.volume = Mathf.Clamp01(currentProfile.voiceVolume);
            SetDistortionState(voiceAudioSource, currentProfile.voiceDistortion);
        }

        if (ambienceAudioSource != null)
        {
            ambienceAudioSource.volume = Mathf.Clamp01(currentProfile.ambienceVolume);
            SetDistortionState(ambienceAudioSource, currentProfile.ambienceDistortion);
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(SoundCrewInteractionAction)}: option={currentPreviewOption} voiceVolume={currentProfile.voiceVolume:F2} ambienceVolume={currentProfile.ambienceVolume:F2} voiceDistortion={currentProfile.voiceDistortion} ambienceDistortion={currentProfile.ambienceDistortion}");
        }
    }

    private void SetDistortionState(AudioSource source, bool enabled)
    {
        if (source == null)
        {
            return;
        }

        AudioDistortionFilter filter = source.GetComponent<AudioDistortionFilter>();
        if (filter == null)
        {
            return;
        }

        filter.enabled = enabled;
    }
}


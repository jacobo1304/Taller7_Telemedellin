using UnityEngine;
using System.Collections;

public class SoundCrewInteractionAction : InteractionActionBase
{
    [System.Serializable]
    private class AudioOptionProfile
    {
        [Range(0f, 1f)] public float presenterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.2f;
        [Range(0f, 1f)] public float ambienceVolume = 0.2f;
    }

    [Header("Micrófono / escucha 3D")]
    [Tooltip("Si se asigna, se moverá su Transform. Si no, se moverá microphoneRig.")]
    [SerializeField] private AudioListener microphoneListener;
    [SerializeField] private Transform microphoneRig;
    [Tooltip("Puntos de escucha por opción (índices 0,1,2)")]
    [SerializeField] private Transform[] listeningOptionSpots = new Transform[3];

    [Header("AudioSources 3D")]
    [SerializeField] private AudioSource presenterVoiceSource;
    [SerializeField] private AudioSource eventMusicSource;
    [SerializeField] private AudioSource ambienceSource;

    [Header("Perfiles por opción (índices 0,1,2)")]
    [SerializeField] private AudioOptionProfile[] optionProfiles = new AudioOptionProfile[3]
    {
        new AudioOptionProfile { presenterVolume = 0f,    musicVolume = 0f,   ambienceVolume = 0f },
        new AudioOptionProfile { presenterVolume = 0.35f, musicVolume = 1f,   ambienceVolume = 0.7f },
        new AudioOptionProfile { presenterVolume = 1f,    musicVolume = 0.2f, ambienceVolume = 0.2f }
    };

    [Header("Transiciones")]
    [SerializeField] private float positionLerpDuration = 0.35f;
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Reset (opcional)")]
    [SerializeField] private bool resetToDefaultOnNoPose = false;
    [SerializeField] private Transform defaultListeningSpot;

    private Coroutine transitionRoutine;
    private int currentPreviewOption = -1;

    public override void PreviewOption(int selectedOptionIndex)
    {
        ApplyOptionState(selectedOptionIndex);
    }

    public override void ResetHoldEffects()
    {
        if (!resetToDefaultOnNoPose)
        {
            return;
        }

        MoveMicTo(defaultListeningSpot);
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
        if (optionProfiles == null || optionProfiles.Length == 0)
        {
            return;
        }

        int clampedOption = Mathf.Clamp(optionIndex, 0, optionProfiles.Length - 1);
        if (clampedOption == currentPreviewOption)
        {
            return;
        }

        currentPreviewOption = clampedOption;

        AudioOptionProfile targetProfile = optionProfiles[clampedOption];
        if (targetProfile == null)
        {
            return;
        }

        Transform targetSpot = null;
        if (listeningOptionSpots != null && clampedOption < listeningOptionSpots.Length)
        {
            targetSpot = listeningOptionSpots[clampedOption];
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(TransitionRoutine(targetProfile, targetSpot));
    }

    private IEnumerator TransitionRoutine(AudioOptionProfile profile, Transform targetSpot)
    {
        Transform movingTransform = microphoneListener != null ? microphoneListener.transform : microphoneRig;

        Vector3 startPos = Vector3.zero;
        Quaternion startRot = Quaternion.identity;
        if (movingTransform != null)
        {
            startPos = movingTransform.position;
            startRot = movingTransform.rotation;
        }

        float startPresenter = presenterVoiceSource != null ? presenterVoiceSource.volume : 0f;
        float startMusic = eventMusicSource != null ? eventMusicSource.volume : 0f;
        float startAmbience = ambienceSource != null ? ambienceSource.volume : 0f;

        float targetPresenter = Mathf.Clamp01(profile.presenterVolume);
        float targetMusic = Mathf.Clamp01(profile.musicVolume);
        float targetAmbience = Mathf.Clamp01(profile.ambienceVolume);

        float duration = Mathf.Max(0.001f, Mathf.Max(positionLerpDuration, fadeDuration));
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float kPos = positionLerpDuration <= 0f ? 1f : Mathf.Clamp01(t / positionLerpDuration);
            float kVol = fadeDuration <= 0f ? 1f : Mathf.Clamp01(t / fadeDuration);

            if (movingTransform != null && targetSpot != null)
            {
                movingTransform.position = Vector3.Lerp(startPos, targetSpot.position, kPos);
                movingTransform.rotation = Quaternion.Slerp(startRot, targetSpot.rotation, kPos);
            }

            if (presenterVoiceSource != null) presenterVoiceSource.volume = Mathf.Lerp(startPresenter, targetPresenter, kVol);
            if (eventMusicSource != null) eventMusicSource.volume = Mathf.Lerp(startMusic, targetMusic, kVol);
            if (ambienceSource != null) ambienceSource.volume = Mathf.Lerp(startAmbience, targetAmbience, kVol);

            yield return null;
        }

        if (movingTransform != null && targetSpot != null)
        {
            movingTransform.position = targetSpot.position;
            movingTransform.rotation = targetSpot.rotation;
        }

        if (presenterVoiceSource != null) presenterVoiceSource.volume = targetPresenter;
        if (eventMusicSource != null) eventMusicSource.volume = targetMusic;
        if (ambienceSource != null) ambienceSource.volume = targetAmbience;

        transitionRoutine = null;
    }

    private void MoveMicTo(Transform targetSpot)
    {
        Transform movingTransform = microphoneListener != null ? microphoneListener.transform : microphoneRig;
        if (movingTransform == null || targetSpot == null)
        {
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        movingTransform.position = targetSpot.position;
        movingTransform.rotation = targetSpot.rotation;

        currentPreviewOption = -1;
    }
}

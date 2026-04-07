using UnityEngine;

public class CameraCrewInteractionAction : InteractionActionBase
{
    [Header("Efecto correcto")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private Transform correctSpot;

    [Header("Efecto incorrecto 1")]
    [SerializeField] private Transform wrongSpot1;

    [Header("Efecto incorrecto 2")]
    [SerializeField] private Transform wrongSpot2;

    [Header("Audio ambiente / voz")]
    [SerializeField] private AudioSource presenterVoice;
    [SerializeField] private AudioSource ambientAudio;
    [SerializeField] private float correctVoiceVolume = 1f;
    [SerializeField] private float wrongVoiceVolume = 0.35f;
    [SerializeField] private float correctAmbientVolume = 0.25f;
    [SerializeField] private float wrongAmbientVolume = 0.8f;

    protected override void ApplyCorrectEffect()
    {
        MoveRigTo(correctSpot);
        SetAudioMix(correctVoiceVolume, correctAmbientVolume);
    }

    protected override void ApplyWrongEffect1()
    {
        MoveRigTo(wrongSpot1);
        SetAudioMix(wrongVoiceVolume, wrongAmbientVolume);
    }

    protected override void ApplyWrongEffect2()
    {
        MoveRigTo(wrongSpot2);
        SetAudioMix(wrongVoiceVolume * 0.8f, Mathf.Clamp01(wrongAmbientVolume + 0.1f));
    }

    private void MoveRigTo(Transform target)
    {
        if (cameraRig == null || target == null) return;
        cameraRig.position = target.position;
        cameraRig.rotation = target.rotation;
    }

    private void SetAudioMix(float voiceVolume, float ambienceVolume)
    {
        if (presenterVoice != null) presenterVoice.volume = Mathf.Clamp01(voiceVolume);
        if (ambientAudio != null) ambientAudio.volume = Mathf.Clamp01(ambienceVolume);
    }
}

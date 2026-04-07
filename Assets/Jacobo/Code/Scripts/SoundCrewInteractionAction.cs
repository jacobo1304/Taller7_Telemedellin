using UnityEngine;

public class SoundCrewInteractionAction : InteractionActionBase
{
    [Header("Fuentes")]
    [SerializeField] private AudioSource presenterMic;
    [SerializeField] private AudioSource eventMusic;
    [SerializeField] private AudioSource ambience;

    [Header("Volúmenes")]
    [SerializeField] private float presenterVolumeCorrect = 1f;
    [SerializeField] private float presenterVolumeWrong = 0.3f;
    [SerializeField] private float musicCorrect = 0.2f;
    [SerializeField] private float musicWrongHigh = 1f;
    [SerializeField] private float ambienceCorrect = 0.2f;
    [SerializeField] private float ambienceWrong = 0.7f;

    protected override void ApplyCorrectEffect()
    {
        if (presenterMic != null) presenterMic.volume = Mathf.Clamp01(presenterVolumeCorrect);
        if (eventMusic != null) eventMusic.volume = Mathf.Clamp01(musicCorrect);
        if (ambience != null) ambience.volume = Mathf.Clamp01(ambienceCorrect);
    }

    // Opción incorrecta: apagar todos los sonidos.
    protected override void ApplyWrongEffect1()
    {
        if (presenterMic != null) presenterMic.volume = 0f;
        if (eventMusic != null) eventMusic.volume = 0f;
        if (ambience != null) ambience.volume = 0f;
    }

    // Opción incorrecta: subir música del evento.
    protected override void ApplyWrongEffect2()
    {
        if (presenterMic != null) presenterMic.volume = Mathf.Clamp01(presenterVolumeWrong);
        if (eventMusic != null) eventMusic.volume = Mathf.Clamp01(musicWrongHigh);
        if (ambience != null) ambience.volume = Mathf.Clamp01(ambienceWrong);
    }
}

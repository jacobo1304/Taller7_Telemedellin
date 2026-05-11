using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Feedback SFX")]
    [SerializeField] private AudioClip[] positiveClips = new AudioClip[0];
    [SerializeField] private AudioClip[] negativeClips = new AudioClip[0];

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Volume")]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        // Implementar patrón Singleton
        if (Instance != null && Instance != this)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SoundManager)}: Ya existe una instancia de SoundManager. Se elimina este duplicado.", this);
            }
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Obtener AudioSource si no está asignada
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Crear AudioSource si no existe
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configurar AudioSource
        audioSource.playOnAwake = false;
        audioSource.volume = sfxVolume;

        if (debugLogs)
        {
            Debug.Log($"{nameof(SoundManager)}: Inicializado correctamente.", this);
        }
    }

    /// <summary>
    /// Reproduce un sonido de feedback positivo (respuesta correcta) aleatorio.
    /// </summary>
    /// <returns>Duración del clip en segundos. Retorna 0 si no hay clips disponibles.</returns>
    public float PlayPositiveFeedback()
    {
        if (positiveClips == null || positiveClips.Length == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SoundManager)}: No hay clips de feedback positivo configurados.", this);
            }
            return 0f;
        }

        AudioClip clip = positiveClips[Random.Range(0, positiveClips.Length)];
        return PlayAudioClip(clip, "Positive Feedback");
    }

    /// <summary>
    /// Reproduce un sonido de feedback negativo (respuesta incorrecta) aleatorio.
    /// </summary>
    /// <returns>Duración del clip en segundos. Retorna 0 si no hay clips disponibles.</returns>
    public float PlayNegativeFeedback()
    {
        if (negativeClips == null || negativeClips.Length == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SoundManager)}: No hay clips de feedback negativo configurados.", this);
            }
            return 0f;
        }

        AudioClip clip = negativeClips[Random.Range(0, negativeClips.Length)];
        return PlayAudioClip(clip, "Negative Feedback");
    }

    /// <summary>
    /// Reproduce un clip de audio específico.
    /// </summary>
    /// <param name="clip">El clip de audio a reproducir.</param>
    /// <param name="clipName">Nombre descriptivo para debug.</param>
    /// <returns>Duración del clip en segundos.</returns>
    private float PlayAudioClip(AudioClip clip, string clipName)
    {
        if (audioSource == null || clip == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(SoundManager)}: AudioSource o AudioClip null para '{clipName}'.", this);
            }
            return 0f;
        }

        audioSource.PlayOneShot(clip, sfxVolume);

        if (debugLogs)
        {
            Debug.Log($"{nameof(SoundManager)}: Reproduciendo '{clipName}' - Duración: {clip.length:F2}s", this);
        }

        return clip.length;
    }

    /// <summary>
    /// Detiene la reproducción de audio actual.
    /// </summary>
    public void StopAudio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Establece el volumen de los efectos de sonido (SFX).
    /// </summary>
    /// <param name="volume">Volumen (0-1).</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = sfxVolume;
        }
    }

    /// <summary>
    /// Obtiene el volumen actual de los SFX.
    /// </summary>
    /// <returns>Volumen (0-1).</returns>
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
}

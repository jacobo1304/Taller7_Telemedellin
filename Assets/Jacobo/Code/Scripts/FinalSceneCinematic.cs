using TMPro;
using UnityEngine;

public class FinalSceneCinematic : MonoBehaviour
{
    [Header("Crew Interactions")]
    [SerializeField] private CameraCrewInteractionAction cameraInteraction;
    [SerializeField] private LightsCrewInteractionAction lightsInteraction;
    [SerializeField] private SoundCrewInteractionAction soundInteraction;
    [SerializeField] private HeadersInteraction headersInteraction;
    [SerializeField] private bool enableSoundInFinalScene = true; // CAMBIO: Habilitado por defecto para CinematicaEscenaFinal

    [Header("UI")]
    [SerializeField] private AudioMeterUI audioMeterUI;

    [Header("Final Scene Clips")]
    [SerializeField] private AudioClip finalVoiceClip;
    [SerializeField] private AudioClip finalAmbienceClip;

    [Header("Final Header Panel")]
    [SerializeField] private GameObject finalHeaderPanel;
    [SerializeField] private TMP_Text finalHeaderText;
    [SerializeField] private bool showHeaderPanelInCinematic = false;

    [Header("Panels to Deactivate on Scene Start")] // CAMBIO: Nueva sección
    [SerializeField] private GameObject panelToDeactivate; // CAMBIO: Panel que se desactivará al iniciar

    [Header("Camera Priority")]
    [SerializeField] private int finalCameraPriority = 1000;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        if (finalHeaderPanel == null && finalHeaderText != null)
        {
            finalHeaderPanel = finalHeaderText.transform.parent != null ? finalHeaderText.transform.parent.gameObject : null;
        }

        if (finalHeaderPanel != null)
        {
            finalHeaderPanel.SetActive(false);
        }

        if (finalHeaderText != null)
        {
            finalHeaderText.text = string.Empty;
        }

        if (panelToDeactivate != null)
        {
            panelToDeactivate.SetActive(false);
        }

        if (headersInteraction != null)
        {
            headersInteraction.DisablePanelActivation();
        }

        // CAMBIO: SIEMPRE detiene el audio anterior al iniciar una nueva cinemática
        if (soundInteraction != null)
        {
            soundInteraction.StopPlayback();
        }
    }

    private void OnDisable()
    {
        // Importante: NO cortar el audio acá.
        // El Timeline/Director suele desactivar este componente al finalizar, y si paramos aquí
        // también se corta el `finalVoiceClip` justo cuando debería escucharse.
        // Si se requiere un corte explícito, usar el Signal que llama `StopFinalSceneAudio()`.
    }

    // Llamar desde Signal al final del Timeline si se requiere un corte explícito.
    public void StopFinalSceneAudio()
    {
        if (soundInteraction != null)
        {
            soundInteraction.StopPlayback();
        }
    }

    /// <summary>
    /// Public signal entry point from Timeline/Director.
    /// Applies the stored crew selections to the final scene.
    /// </summary>
    public void ApplyFinalScene()
    {
        if (debugLogs)
        {
            Debug.Log($"{nameof(FinalSceneCinematic)}: ApplyFinalScene invoked.", this);
        }

        ApplyCameraSelection();
        ApplyLightsSelection();
        ApplySoundSelection();
        ApplyFinalHeaders();
    }

    // Alias name in case the Timeline signal is connected with a different label.
    public void OnDirectorSignal()
    {
        ApplyFinalScene();
    }

    public void PlayFinalScene()
    {
        ApplyFinalScene();
    }

    private void ApplyCameraSelection()
    {
        if (cameraInteraction == null)
        {
            return;
        }

        cameraInteraction.RestoreStoredSelectionWithPriority(finalCameraPriority);

        if (debugLogs)
        {
            MonoBehaviour selectedCamera = cameraInteraction.GetStoredSelectedCamera();
            Debug.Log($"{nameof(FinalSceneCinematic)}: Camera applied. selectedCamera={(selectedCamera == null ? "null" : selectedCamera.name)} priority={finalCameraPriority}", this);
        }
    }

    private void ApplyLightsSelection()
    {
        if (lightsInteraction == null)
        {
            return;
        }

        lightsInteraction.RestoreStoredSelection();

        if (debugLogs)
        {
            Debug.Log($"{nameof(FinalSceneCinematic)}: Lights applied. storedIndex={lightsInteraction.StoredSelectedOptionIndex}", this);
        }
    }

    private void ApplySoundSelection()
    {
        // CAMBIO: Solo aplica sonido si está habilitado en la escena final
        if (!enableSoundInFinalScene)
        {
            return;
        }

        if (soundInteraction == null)
        {
            return;
        }

        // En la escena final los clips NO deben quedar en loop.
        soundInteraction.SetLooping(false);

        soundInteraction.RestoreStoredSelection();
        soundInteraction.SetClipsAndRestart(
            finalVoiceClip,
            finalAmbienceClip
        );

        if (debugLogs)
        {
            if (finalVoiceClip == null)
            {
                Debug.LogWarning($"{nameof(FinalSceneCinematic)}: finalVoiceClip is NULL. Voice will not play unless VoiceAudioSource already has a clip.", this);
            }

            if (soundInteraction.VoiceAudioSource == null)
            {
                Debug.LogWarning($"{nameof(FinalSceneCinematic)}: soundInteraction.VoiceAudioSource is NULL (not assigned in inspector?)", this);
            }
        }

        if (audioMeterUI != null && soundInteraction.VoiceAudioSource != null)
        {
            audioMeterUI.SetAudioSource(soundInteraction.VoiceAudioSource);
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(FinalSceneCinematic)}: Sound applied. storedIndex={soundInteraction.StoredSelectedOptionIndex}", this);
        }
    }

    private void ApplyFinalHeaders()
    {
        // CAMBIO: Solo activa el panel si el flag está habilitado (por defecto desactivado)
        if (finalHeaderPanel != null && showHeaderPanelInCinematic)
        {
            finalHeaderPanel.SetActive(true);
        }

        if (finalHeaderText == null)
        {
            return;
        }

        string finalText = string.Empty;
        if (headersInteraction != null)
        {
            finalText = headersInteraction.GetStoredSelectedHeaderText();
        }

        finalHeaderText.text = finalText ?? string.Empty;

        if (debugLogs)
        {
            Debug.Log($"{nameof(FinalSceneCinematic)}: Final header text applied='{finalHeaderText.text}'", this);
        }
    }
}

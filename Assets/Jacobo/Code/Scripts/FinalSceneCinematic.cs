using TMPro;
using UnityEngine;

public class FinalSceneCinematic : MonoBehaviour
{
    [Header("Crew Interactions")]
    [SerializeField] private CameraCrewInteractionAction cameraInteraction;
    [SerializeField] private LightsCrewInteractionAction lightsInteraction;
    [SerializeField] private SoundCrewInteractionAction soundInteraction;
    [SerializeField] private HeadersInteraction headersInteraction;

    [Header("Final Header Panel")]
    [SerializeField] private GameObject finalHeaderPanel;
    [SerializeField] private TMP_Text finalHeaderText;

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
        if (soundInteraction == null)
        {
            return;
        }

        soundInteraction.RestoreStoredSelection();

        if (debugLogs)
        {
            Debug.Log($"{nameof(FinalSceneCinematic)}: Sound applied. storedIndex={soundInteraction.StoredSelectedOptionIndex}", this);
        }
    }

    private void ApplyFinalHeaders()
    {
        if (finalHeaderPanel != null)
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

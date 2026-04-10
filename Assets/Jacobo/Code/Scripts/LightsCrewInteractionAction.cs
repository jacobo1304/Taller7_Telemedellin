using System.Collections;
using UnityEngine;

public class LightsCrewInteractionAction : InteractionActionBase
{
    [Header("Pose correcta (de esta interacción)")]
    [Tooltip("Índice de la pose ganadora según el array de Pose Options de esta interacción.")]
    [SerializeField] private int winningPoseOptionIndex = 1;

    [Header("Luces")]
    [SerializeField] private Light correctLight;
    [SerializeField] private Light incorrectLight1;
    [SerializeField] private Light incorrectLight2;

    [Header("Intensidades")]
    [SerializeField] private float correctLightTargetIntensity = 1f;
    [SerializeField] private float incorrectLight1Intensity = 1f;
    [SerializeField] private float incorrectLight2Intensity = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine lightFadeCoroutine;

    protected override int ResolveCorrectOptionIndex()
    {
        if (PoseOptionsCount <= 0)
        {
            return Mathf.Max(0, winningPoseOptionIndex);
        }

        return Mathf.Clamp(winningPoseOptionIndex, 0, PoseOptionsCount - 1);
    }

    protected override void ApplyCorrectEffect()
    {
        FadePoseLights(correctLightTargetIntensity, 0f, 0f);
    }

    // Opción incorrecta 1: activar ambas luces incorrectas.
    protected override void ApplyWrongEffect1()
    {
        FadePoseLights(0f, incorrectLight1Intensity, incorrectLight2Intensity);
    }

    // Opción incorrecta 2: activar solo la segunda luz incorrecta.
    protected override void ApplyWrongEffect2()
    {
        FadePoseLights(0f, 0f, incorrectLight2Intensity);
    }

    public void ResetPoseLights()
    {
        FadePoseLights(0f, 0f, 0f);
    }

    public override void ResetHoldEffects()
    {
        ResetPoseLights();
    }

    private void FadePoseLights(float targetCorrect, float targetWrong1, float targetWrong2)
    {
        if (lightFadeCoroutine != null)
        {
            StopCoroutine(lightFadeCoroutine);
        }

        lightFadeCoroutine = StartCoroutine(FadePoseLightsCoroutine(targetCorrect, targetWrong1, targetWrong2));
    }

    private IEnumerator FadePoseLightsCoroutine(float targetCorrect, float targetWrong1, float targetWrong2)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.001f, fadeDuration);

        float startCorrect = correctLight != null ? correctLight.intensity : 0f;
        float startWrong1 = incorrectLight1 != null ? incorrectLight1.intensity : 0f;
        float startWrong2 = incorrectLight2 != null ? incorrectLight2.intensity : 0f;

        if (correctLight != null && targetCorrect > 0f)
        {
            correctLight.enabled = true;
        }

        if (incorrectLight1 != null && targetWrong1 > 0f)
        {
            incorrectLight1.enabled = true;
        }

        if (incorrectLight2 != null && targetWrong2 > 0f)
        {
            incorrectLight2.enabled = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (correctLight != null)
            {
                correctLight.intensity = Mathf.Lerp(startCorrect, targetCorrect, t);
            }

            if (incorrectLight1 != null)
            {
                incorrectLight1.intensity = Mathf.Lerp(startWrong1, targetWrong1, t);
            }

            if (incorrectLight2 != null)
            {
                incorrectLight2.intensity = Mathf.Lerp(startWrong2, targetWrong2, t);
            }

            yield return null;
        }

        if (correctLight != null)
        {
            correctLight.intensity = targetCorrect;
            correctLight.enabled = targetCorrect > 0f;
        }

        if (incorrectLight1 != null)
        {
            incorrectLight1.intensity = targetWrong1;
            incorrectLight1.enabled = targetWrong1 > 0f;
        }

        if (incorrectLight2 != null)
        {
            incorrectLight2.intensity = targetWrong2;
            incorrectLight2.enabled = targetWrong2 > 0f;
        }

        lightFadeCoroutine = null;
    }
}

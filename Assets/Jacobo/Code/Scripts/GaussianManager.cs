using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianManager : MonoBehaviour
{
    public enum BlurMode
    {
        Gaussian,
        Bokeh
    }

    [Header("Volume")]
    [SerializeField] private Volume blurVolume;
    [SerializeField] private BlurMode blurMode = BlurMode.Gaussian;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxVolumeWeight = 1f;

    [Header("Gaussian Settings")]
    [SerializeField, Min(0f)] private float gaussianStart = 0.1f;
    [SerializeField, Min(0f)] private float gaussianEnd = 8f;
    [SerializeField, Range(0.5f, 2f)] private float gaussianMaxRadius = 1f;

    [Header("Bokeh Settings")]
    [SerializeField, Min(0.01f)] private float bokehFocusDistance = 0.2f;
    [SerializeField, Range(1f, 32f)] private float bokehAperture = 16f;
    [SerializeField, Min(1f)] private float bokehFocalLength = 50f;

    private DepthOfField dof;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        EnsureSetup();
    }

    public void EnableBlur()
    {
        FadeBlur(true);
    }

    public void DisableBlur()
    {
        FadeBlur(false);
    }

    public void FadeBlur(bool enabled)
    {
        EnsureSetup();

        if (blurVolume == null || dof == null)
        {
            Debug.LogWarning($"{nameof(GaussianManager)}: Volume o DepthOfField no configurado.", this);
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        float targetWeight = enabled ? maxVolumeWeight : 0f;
        float duration = enabled ? fadeInDuration : fadeOutDuration;

        if (enabled)
        {
            blurVolume.enabled = true;
            ApplyDepthOfFieldSettings();
        }

        fadeRoutine = StartCoroutine(FadeWeightRoutine(targetWeight, duration, disableVolumeWhenDone: !enabled));
    }

    public void SetBlur(float normalizedWeight)
    {
        EnsureSetup();

        if (blurVolume == null || dof == null)
        {
            return;
        }

        ApplyDepthOfFieldSettings();

        float target = Mathf.Clamp01(normalizedWeight) * maxVolumeWeight;
        blurVolume.weight = target;
        blurVolume.enabled = target > 0f;
    }

    private IEnumerator FadeWeightRoutine(float targetWeight, float duration, bool disableVolumeWhenDone)
    {
        float start = blurVolume.weight;

        if (duration <= 0f)
        {
            blurVolume.weight = targetWeight;
            if (disableVolumeWhenDone && targetWeight <= 0f)
            {
                blurVolume.enabled = false;
            }
            fadeRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            blurVolume.weight = Mathf.Lerp(start, targetWeight, k);
            yield return null;
        }

        blurVolume.weight = targetWeight;

        if (disableVolumeWhenDone && targetWeight <= 0f)
        {
            blurVolume.enabled = false;
        }

        fadeRoutine = null;
    }

    private void EnsureSetup()
    {
        if (blurVolume == null)
        {
            return;
        }

        if (dof == null)
        {
            blurVolume.profile.TryGet(out dof);
        }

        if (dof != null)
        {
            dof.active = true;
        }
    }

    private void ApplyDepthOfFieldSettings()
    {
        if (dof == null)
        {
            return;
        }

        if (blurMode == BlurMode.Gaussian)
        {
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(gaussianStart);
            dof.gaussianEnd.Override(gaussianEnd);
            dof.gaussianMaxRadius.Override(gaussianMaxRadius);
            return;
        }

        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(bokehFocusDistance);
        dof.aperture.Override(bokehAperture);
        dof.focalLength.Override(bokehFocalLength);
    }
}

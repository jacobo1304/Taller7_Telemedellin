using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeManager : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume targetVolume;
    [SerializeField, Range(0f, 1f)] private float shownWeight = 1f;
    [SerializeField] private bool disableGameObjectWhenHidden = true;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (targetVolume == null)
        {
            targetVolume = GetComponent<Volume>();
        }
    }

    public void Show()
    {
        FadeToWeight(shownWeight, fadeInDuration);
    }

    public void Hide()
    {
        FadeToWeight(0f, fadeOutDuration);
    }

    public void FadeIn()
    {
        Show();
    }

    public void FadeOut()
    {
        Hide();
    }

    public void SetWeight(float normalizedWeight)
    {
        if (targetVolume == null)
        {
            return;
        }

        StopFadeRoutineIfNeeded();

        float clamped = Mathf.Clamp01(normalizedWeight);
        targetVolume.enabled = clamped > 0f;
        targetVolume.weight = clamped;

        if (disableGameObjectWhenHidden)
        {
            targetVolume.gameObject.SetActive(clamped > 0f);
        }
    }

    public void FadeToShown(bool shown)
    {
        FadeToWeight(shown ? shownWeight : 0f, shown ? fadeInDuration : fadeOutDuration);
    }

    public void FadeToWeight(float targetWeight)
    {
        FadeToWeight(targetWeight, targetWeight > 0f ? fadeInDuration : fadeOutDuration);
    }

    public void FadeToWeight(float targetWeight, float duration)
    {
        if (targetVolume == null)
        {
            return;
        }

        StopFadeRoutineIfNeeded();
        fadeRoutine = StartCoroutine(FadeWeightRoutine(Mathf.Clamp01(targetWeight), Mathf.Max(0f, duration)));
    }

    private IEnumerator FadeWeightRoutine(float targetWeight, float duration)
    {
        if (disableGameObjectWhenHidden && !targetVolume.gameObject.activeSelf)
        {
            targetVolume.gameObject.SetActive(true);
        }

        targetVolume.enabled = true;
        float start = targetVolume.weight;

        if (duration <= 0f)
        {
            ApplyFinalState(targetWeight);
            fadeRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            targetVolume.weight = Mathf.Lerp(start, targetWeight, k);
            yield return null;
        }

        ApplyFinalState(targetWeight);
        fadeRoutine = null;
    }

    private void ApplyFinalState(float weight)
    {
        targetVolume.weight = weight;
        bool visible = weight > 0f;
        targetVolume.enabled = visible;

        if (disableGameObjectWhenHidden)
        {
            targetVolume.gameObject.SetActive(visible);
        }
    }

    private void StopFadeRoutineIfNeeded()
    {
        if (fadeRoutine == null)
        {
            return;
        }

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }
}

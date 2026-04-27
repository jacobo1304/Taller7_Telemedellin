using System.Collections;
using UnityEngine;

public class AmbienceLightModifier : MonoBehaviour
{
    [Header("Lights")]
    [SerializeField] private Light[] targetLights;
    [SerializeField] private float nightIntensity = 0.35f;
    [SerializeField] private float dawnIntensity = 1.0f;

    [Header("Sky Material (optional)")]
    [SerializeField] private Material skyMaterialOverride;
    [SerializeField] private string skyColorProperty = "_Tint";
    [SerializeField] private Color nightSkyColor = new Color(0.16f, 0.20f, 0.28f, 1f);
    [SerializeField] private Color dawnSkyColor = new Color(0.75f, 0.82f, 0.95f, 1f);
    [SerializeField] private bool lerpSkyColor = true;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 1.25f;

    private Coroutine transitionRoutine;

    public void TriggerDawn()
    {
        SetDawnState(true);
    }

    public void TriggerNight()
    {
        SetDawnState(false);
    }

    public void SetDawnState(bool isDawn)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        transitionRoutine = StartCoroutine(TransitionRoutine(isDawn));
    }

    private IEnumerator TransitionRoutine(bool isDawn)
    {
        float targetIntensity = isDawn ? dawnIntensity : nightIntensity;

        float[] startIntensities = new float[targetLights == null ? 0 : targetLights.Length];
        if (targetLights != null)
        {
            for (int i = 0; i < targetLights.Length; i++)
            {
                startIntensities[i] = targetLights[i] != null ? targetLights[i].intensity : 0f;
            }
        }

        Material sky = ResolveSkyMaterial();
        bool canLerpSky = lerpSkyColor && sky != null && sky.HasProperty(skyColorProperty);
        Color startSkyColor = canLerpSky ? sky.GetColor(skyColorProperty) : Color.black;
        Color targetSkyColor = isDawn ? dawnSkyColor : nightSkyColor;

        if (transitionDuration <= 0f)
        {
            ApplyLightIntensity(targetIntensity);
            if (canLerpSky)
            {
                sky.SetColor(skyColorProperty, targetSkyColor);
                DynamicGI.UpdateEnvironment();
            }
            transitionRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / transitionDuration);

            if (targetLights != null)
            {
                for (int i = 0; i < targetLights.Length; i++)
                {
                    if (targetLights[i] == null)
                    {
                        continue;
                    }

                    targetLights[i].intensity = Mathf.Lerp(startIntensities[i], targetIntensity, k);
                }
            }

            if (canLerpSky)
            {
                sky.SetColor(skyColorProperty, Color.Lerp(startSkyColor, targetSkyColor, k));
            }

            yield return null;
        }

        ApplyLightIntensity(targetIntensity);
        if (canLerpSky)
        {
            sky.SetColor(skyColorProperty, targetSkyColor);
            DynamicGI.UpdateEnvironment();
        }

        transitionRoutine = null;
    }

    private void ApplyLightIntensity(float intensity)
    {
        if (targetLights == null)
        {
            return;
        }

        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
            {
                targetLights[i].intensity = intensity;
            }
        }
    }

    private Material ResolveSkyMaterial()
    {
        if (skyMaterialOverride != null)
        {
            return skyMaterialOverride;
        }

        return RenderSettings.skybox;
    }
}

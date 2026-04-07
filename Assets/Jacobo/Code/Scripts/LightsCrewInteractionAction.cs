using UnityEngine;

public class LightsCrewInteractionAction : InteractionActionBase
{
    [Header("Luces")]
    [SerializeField] private Light frontalLight;
    [SerializeField] private Light[] presenterFillLights;
    [SerializeField] private Light[] audienceLights;

    [Header("Intensidades")]
    [SerializeField] private float frontalIntensityCorrect = 1.3f;
    [SerializeField] private float fillIntensityCorrect = 0.8f;
    [SerializeField] private float audienceIntensityWrong = 1.2f;

    protected override void ApplyCorrectEffect()
    {
        if (frontalLight != null)
        {
            frontalLight.enabled = true;
            frontalLight.intensity = frontalIntensityCorrect;
        }

        SetLights(presenterFillLights, true, fillIntensityCorrect);
        SetLights(audienceLights, false, 0f);
    }

    // Opción incorrecta: apagar todas las luces.
    protected override void ApplyWrongEffect1()
    {
        if (frontalLight != null)
        {
            frontalLight.enabled = false;
        }

        SetLights(presenterFillLights, false, 0f);
        SetLights(audienceLights, false, 0f);
    }

    // Opción incorrecta: apuntar luces al público.
    protected override void ApplyWrongEffect2()
    {
        if (frontalLight != null)
        {
            frontalLight.enabled = false;
        }

        SetLights(presenterFillLights, false, 0f);
        SetLights(audienceLights, true, audienceIntensityWrong);
    }

    private void SetLights(Light[] lights, bool enabledState, float intensity)
    {
        if (lights == null) return;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].enabled = enabledState;
            lights[i].intensity = intensity;
        }
    }
}

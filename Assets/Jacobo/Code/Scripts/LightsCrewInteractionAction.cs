using UnityEngine;

public class LightsCrewInteractionAction : InteractionActionBase
{
    [Header("Luces")]
    [SerializeField] private Light LuzCorrecta;
    [SerializeField] private Light LuzIncorrecta1;
    [SerializeField] private Light LuzIncorrecta2;

    [Header("Intensidades")]
    [SerializeField] private float IntensityCorrect = 1.3f;
    [SerializeField] private float IntensityIncorrect1 = 0.8f;
    [SerializeField] private float IntensityIncorrect2 = 1.2f;

    protected override void ApplyCorrectEffect()
    {
        if (LuzCorrecta != null)
        {
            SetLights(LuzCorrecta, true, IntensityCorrect);
        }

        SetLights(LuzIncorrecta1, false, 0f);
        SetLights(LuzIncorrecta2, false, 0f);
    }

    
    // Opción incorrecta: apagar todas las luces.
    protected override void ApplyWrongEffect1()
    {
       SetLights(LuzIncorrecta1, true, IntensityIncorrect1);
         SetLights(LuzIncorrecta2, false, 0f);

            if (LuzCorrecta != null)
            {
               SetLights(LuzCorrecta, false, 0f);
            }
    }

    // Opción incorrecta: apuntar luces al público.
    protected override void ApplyWrongEffect2()
    {
        SetLights(LuzIncorrecta1, false, 0f);
        SetLights(LuzIncorrecta2, true, IntensityIncorrect2);
        if (LuzCorrecta != null)
        {
            SetLights(LuzCorrecta, false, 0f);
        }
    }

    private void SetLights(Light light, bool enabledState, float intensity)
    {
        if (light == null) return;

        light.enabled = enabledState;
        light.intensity = intensity;
    }
}
        
        
    


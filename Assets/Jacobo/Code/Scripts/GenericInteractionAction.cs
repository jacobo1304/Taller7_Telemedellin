using UnityEngine;
using UnityEngine.Events;

public class GenericInteractionAction : InteractionActionBase
{
    [Header("Eventos genéricos")]
    [SerializeField] private UnityEvent onApplyCorrect;
    [SerializeField] private UnityEvent onApplyWrong1;
    [SerializeField] private UnityEvent onApplyWrong2;

    protected override void ApplyCorrectEffect()
    {
        onApplyCorrect?.Invoke();
    }

    protected override void ApplyWrongEffect1()
    {
        onApplyWrong1?.Invoke();
    }

    protected override void ApplyWrongEffect2()
    {
        onApplyWrong2?.Invoke();
    }
}

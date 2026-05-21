using UnityEngine;

public class CameraCrewInteractionAction : InteractionActionBase
{
    [Header("Cámara (Cinemachine)")]
    [Tooltip("Opciones de cámara por índice de pose (0,1,2,3).")]
    [SerializeField] private MonoBehaviour[] cameraOptionVirtualCameras = new MonoBehaviour[4];
    [SerializeField] private int activePriority = 40;
    [SerializeField] private int inactivePriority = 0;

    [Header("Reset (opcional)")]
    [SerializeField] private bool resetToDefaultOnNoPose = false;
    [SerializeField] private MonoBehaviour defaultVirtualCamera;

    private bool hasPendingSwitch;
    private int pendingOptionIndex = -1;

    private void OnEnable()
    {
        if (!hasPendingSwitch)
        {
            return;
        }

        int optionIndex = pendingOptionIndex;
        hasPendingSwitch = false;
        pendingOptionIndex = -1;
        SetActiveCameraForOption(optionIndex);
    }

    public override void ResetHoldEffects()
    {
        if (!resetToDefaultOnNoPose)
        {
            return;
        }

        SetOnlyActiveCamera(defaultVirtualCamera);
    }

    public override void PreviewOption(int selectedOptionIndex)
    {
        SetActiveCameraForOption(selectedOptionIndex);
    }

    protected override void ApplyCorrectEffect()
    {
        SetActiveCameraForOption(CorrectOptionIndex);
    }

    protected override void ApplyWrongEffect1()
    {
        SetActiveCameraForOption(WrongOption1Index);
    }

    protected override void ApplyWrongEffect2()
    {
        SetActiveCameraForOption(WrongOption2Index);
    }

    private void SetActiveCameraForOption(int optionIndex)
    {
        if (cameraOptionVirtualCameras == null || optionIndex < 0 || optionIndex >= cameraOptionVirtualCameras.Length)
        {
            return;
        }

        MonoBehaviour selectedCamera = cameraOptionVirtualCameras[optionIndex];
        if (selectedCamera == null)
        {
            return;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            hasPendingSwitch = true;
            pendingOptionIndex = optionIndex;
            return;
        }

        SetOnlyActiveCamera(selectedCamera);
    }

    public MonoBehaviour GetCameraOption(int optionIndex)
    {
        if (cameraOptionVirtualCameras == null || optionIndex < 0 || optionIndex >= cameraOptionVirtualCameras.Length)
        {
            return null;
        }

        return cameraOptionVirtualCameras[optionIndex];
    }

    public MonoBehaviour GetStoredSelectedCamera()
    {
        return GetCameraOption(StoredSelectedOptionIndex);
    }

    public void RestoreStoredSelectionWithPriority(int highPriority)
    {
        MonoBehaviour selectedCamera = GetStoredSelectedCamera();
        if (selectedCamera == null)
        {
            return;
        }

        ApplyCameraPriorities(selectedCamera, highPriority, inactivePriority);
    }

    private void SetOnlyActiveCamera(MonoBehaviour selectedCamera)
    {
        ApplyCameraPriorities(selectedCamera, activePriority, inactivePriority);
    }

    private void ApplyCameraPriorities(MonoBehaviour selectedCamera, int selectedPriority, int nonSelectedPriority)
    {
        if (selectedCamera == null)
        {
            return;
        }

        if (defaultVirtualCamera != null)
        {
            SetCameraPriority(defaultVirtualCamera, selectedCamera == defaultVirtualCamera ? selectedPriority : nonSelectedPriority);
        }

        for (int i = 0; i < cameraOptionVirtualCameras.Length; i++)
        {
            MonoBehaviour cam = cameraOptionVirtualCameras[i];
            if (cam == null)
            {
                continue;
            }

            int targetPriority = cam == selectedCamera ? selectedPriority : nonSelectedPriority;
            SetCameraPriority(cam, targetPriority);
        }
    }

    private static void SetCameraPriority(MonoBehaviour cameraComponent, int priority)
    {
        if (cameraComponent == null)
        {
            return;
        }

        var type = cameraComponent.GetType();

        var priorityProp = type.GetProperty("Priority");
        if (priorityProp != null && priorityProp.CanWrite)
        {
            if (TrySetPriorityValue(priorityProp.PropertyType, priorityProp.GetValue(cameraComponent), priority, out object updatedPropValue))
            {
                priorityProp.SetValue(cameraComponent, updatedPropValue);
                return;
            }
        }

        var priorityField = type.GetField("m_Priority");
        if (priorityField != null)
        {
            if (TrySetPriorityValue(priorityField.FieldType, priorityField.GetValue(cameraComponent), priority, out object updatedFieldValue))
            {
                priorityField.SetValue(cameraComponent, updatedFieldValue);
                return;
            }
        }

        var directPriorityField = type.GetField("Priority");
        if (directPriorityField != null)
        {
            if (TrySetPriorityValue(directPriorityField.FieldType, directPriorityField.GetValue(cameraComponent), priority, out object updatedDirectFieldValue))
            {
                directPriorityField.SetValue(cameraComponent, updatedDirectFieldValue);
            }
        }
    }

    private static bool TrySetPriorityValue(System.Type memberType, object currentValue, int priority, out object updatedValue)
    {
        updatedValue = currentValue;

        if (memberType == typeof(int))
        {
            updatedValue = priority;
            return true;
        }

        if (currentValue == null)
        {
            return false;
        }

        var wrappedType = currentValue.GetType();

        var valueProp = wrappedType.GetProperty("Value");
        if (valueProp != null && valueProp.CanWrite && valueProp.PropertyType == typeof(int))
        {
            valueProp.SetValue(currentValue, priority);
            updatedValue = currentValue;
            return true;
        }

        var valueField = wrappedType.GetField("Value");
        if (valueField != null && valueField.FieldType == typeof(int))
        {
            valueField.SetValue(currentValue, priority);
            updatedValue = currentValue;
            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        hasPendingSwitch = false;
        pendingOptionIndex = -1;
    }
}

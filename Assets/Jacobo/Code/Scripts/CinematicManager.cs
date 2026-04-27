using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class CinematicManager : MonoBehaviour
{
    [Serializable]
    public class CinematicEntry
    {
        public string id;
        public PlayableDirector director;
        [Tooltip("Referencia opcional a una cámara Cinemachine para esta cinemática.\nPuede ser cualquier componente con propiedad 'Priority' o campo 'm_Priority'.")]
        public MonoBehaviour virtualCamera;
    }

    [Serializable]
    public class IntEvent : UnityEvent<int> { }

    [Header("Cinematics")]
    [SerializeField] private List<CinematicEntry> cinematics = new List<CinematicEntry>();
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool autoPlayOnStart = false;

    [Header("Camera Priority")]
    [SerializeField] private int activeCameraPriority = 50;
    [SerializeField] private int inactiveCameraPriority = 0;

    [Header("Events")]
    [SerializeField] private IntEvent onCinematicChanged;

    private int currentIndex = -1;

    public int CurrentIndex => currentIndex;

    private void Start()
    {
        if (cinematics.Count == 0)
        {
            return;
        }

        SetCurrentIndex(Mathf.Clamp(startIndex, 0, cinematics.Count - 1), false);

        if (autoPlayOnStart)
        {
            PlayCurrent();
        }
    }

    public void PlayCurrent()
    {
        var entry = GetCurrent();
        if (entry == null)
        {
            return;
        }

        ApplyCameraPriority(currentIndex);
        entry.director?.Play();
    }

    public void StopCurrent()
    {
        var entry = GetCurrent();
        if (entry == null)
        {
            return;
        }

        entry.director?.Stop();

        if (entry.director != null)
        {
            entry.director.gameObject.SetActive(false);
        }
    }

    public void StopAll()
    {
        for (int i = 0; i < cinematics.Count; i++)
        {
            var entry = cinematics[i];
            if (entry == null || entry.director == null)
            {
                continue;
            }

            entry.director.Stop();
            entry.director.gameObject.SetActive(false);

            if (entry.virtualCamera != null)
            {
                SetCameraPriority(entry.virtualCamera, inactiveCameraPriority);
            }
        }
    }

    public void PlayNext()
    {
        if (cinematics.Count == 0)
        {
            return;
        }

        int next = currentIndex + 1;
        if (next >= cinematics.Count)
        {
            next = 0;
        }

        SetCurrentIndex(next, true);
    }

    public void PlayPrevious()
    {
        if (cinematics.Count == 0)
        {
            return;
        }

        int prev = currentIndex - 1;
        if (prev < 0)
        {
            prev = cinematics.Count - 1;
        }

        SetCurrentIndex(prev, true);
    }

    public void PlayByIndex(int index)
    {
        if (cinematics.Count == 0)
        {
            return;
        }

        int clamped = Mathf.Clamp(index, 0, cinematics.Count - 1);
        SetCurrentIndex(clamped, true);
    }

    public void PlayById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        for (int i = 0; i < cinematics.Count; i++)
        {
            if (string.Equals(cinematics[i].id, id, StringComparison.OrdinalIgnoreCase))
            {
                SetCurrentIndex(i, true);
                return;
            }
        }
    }

    // Compatibilidad con eventos de poses/respuestas
    public void OnPoseResolved()
    {
        PlayNext();
    }

    public void OnPoseResolved(InteractionType _)
    {
        PlayNext();
    }

    public void OnAnswerResult(bool _)
    {
        PlayNext();
    }

    private CinematicEntry GetCurrent()
    {
        if (currentIndex < 0 || currentIndex >= cinematics.Count)
        {
            return null;
        }

        return cinematics[currentIndex];
    }

    private void SetCurrentIndex(int nextIndex, bool playNew)
    {
        if (nextIndex < 0 || nextIndex >= cinematics.Count)
        {
            return;
        }

        int previousIndex = currentIndex;
        if (previousIndex >= 0 && previousIndex < cinematics.Count)
        {
            var previous = cinematics[previousIndex];
            if (previous != null)
            {
                previous.director?.Stop();
                if (previous.director != null)
                {
                    previous.director.gameObject.SetActive(false);
                }

                if (previous.virtualCamera != null)
                {
                    SetCameraPriority(previous.virtualCamera, inactiveCameraPriority);
                }
            }
        }

        currentIndex = nextIndex;

        var current = GetCurrent();
        if (current == null)
        {
            return;
        }

        if (current.director != null)
        {
            current.director.gameObject.SetActive(true);
        }

        ApplyCameraPriority(currentIndex);
        onCinematicChanged?.Invoke(currentIndex);

        if (playNew)
        {
            current.director?.Play();
        }
    }

    private void ApplyCameraPriority(int activeIndex)
    {
        for (int i = 0; i < cinematics.Count; i++)
        {
            var entry = cinematics[i];
            if (entry == null || entry.virtualCamera == null)
            {
                continue;
            }

            int targetPriority = i == activeIndex ? activeCameraPriority : inactiveCameraPriority;
            SetCameraPriority(entry.virtualCamera, targetPriority);
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
        if (priorityProp != null && priorityProp.CanWrite && priorityProp.PropertyType == typeof(int))
        {
            priorityProp.SetValue(cameraComponent, priority);
            return;
        }

        var priorityField = type.GetField("m_Priority");
        if (priorityField != null && priorityField.FieldType == typeof(int))
        {
            priorityField.SetValue(cameraComponent, priority);
        }
    }
}

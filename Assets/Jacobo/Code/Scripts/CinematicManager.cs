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

    [Header("Background Music")]
    [Tooltip("AudioSource de la música de fondo que debe sonar DURANTE las cinemáticas (excepto en índices de excepción).")]
    [SerializeField] private AudioSource backgroundMusicSource;

    [Tooltip("Índices de cinemáticas en las que la música de fondo debe estar en pausa (por ejemplo: intro y cierre).")]
    [SerializeField] private int[] backgroundMusicPausedCinematicIndices = Array.Empty<int>();

    [Tooltip("Si está activo, la música de fondo se pausa cuando NO hay una cinemática reproduciéndose (típicamente durante interactions/preguntas).")]
    [SerializeField] private bool pauseBackgroundMusicWhenNoCinematicPlaying = true;

    [SerializeField] bool debugLogs = false;    
    private int currentIndex = -1;

    private bool backgroundMusicManuallyPaused;
    private bool backgroundMusicInteractionPaused;
    private bool lastDesiredBackgroundMusicState;

    private readonly Dictionary<PlayableDirector, int> directorToIndex = new Dictionary<PlayableDirector, int>();
    private bool directorEventsHooked;

    public int CurrentIndex => currentIndex;

    public bool IsCurrentCinematicPlaying
    {
        get
        {
            var entry = GetCurrent();
            return entry != null && entry.director != null && entry.director.state == PlayState.Playing;
        }
    }

    private void Start()
    {
        if (cinematics.Count == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(CinematicManager)}: No hay cinemáticas configuradas.", this);
            }
            return;
        }

        // Default: por especificación, la intro (0) y el cierre (último) son excepciones.
        if (backgroundMusicPausedCinematicIndices == null || backgroundMusicPausedCinematicIndices.Length == 0)
        {
            backgroundMusicPausedCinematicIndices = cinematics.Count <= 1
                ? new[] { 0 }
                : new[] { 0, cinematics.Count - 1 };
        }

        RebuildDirectorIndex();
        HookDirectorEvents();

        SetCurrentIndex(Mathf.Clamp(startIndex, 0, cinematics.Count - 1), false);

        UpdateBackgroundMusicState(forceApply: true);

        if (autoPlayOnStart)
        {
            PlayCurrent();
        }
    }

    private void OnDestroy()
    {
        UnhookDirectorEvents();
    }

    public void PlayCurrent()
    {
        if(debugLogs)
        {
            Debug.Log($"{nameof(CinematicManager)}: PlayCurrent called. CurrentIndex={currentIndex}", this);
        }
        var entry = GetCurrent();
        if (entry == null)
        {
            return;
        }

        ApplyCameraPriority(currentIndex);
        if (debugLogs)
        {
            Debug.Log($"{nameof(CinematicManager)}: Playing current director={(entry.director == null ? "null" : entry.director.name)}", this);
        }
        entry.director?.Play();

        // Volver de interaction: al iniciar cinemática se limpia la pausa por interacción.
        backgroundMusicInteractionPaused = false;

        UpdateBackgroundMusicState(forceApply: true);
    }

    public void ReplayCurrent()
    {
        var entry = GetCurrent();
        if (entry == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(CinematicManager)}: ReplayCurrent ignorado. Current entry es null (index={currentIndex}).", this);
            }
            return;
        }

        if (entry.director == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(CinematicManager)}: ReplayCurrent ignorado. Director null en index={currentIndex}.", this);
            }
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(CinematicManager)}: ReplayCurrent called for director={entry.director.name} at index={currentIndex}", this);
        }

        ApplyCameraPriority(currentIndex);
        entry.director.gameObject.SetActive(true);
        entry.director.Stop();
        entry.director.time = 0d;
        entry.director.Evaluate();
        entry.director.Play();

        backgroundMusicInteractionPaused = false;

        UpdateBackgroundMusicState(forceApply: true);
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

        UpdateBackgroundMusicState(forceApply: true);
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

        UpdateBackgroundMusicState(forceApply: true);
    }

    public void PlayNext()
    {
        if (debugLogs)
        {
            Debug.Log($"{nameof(CinematicManager)}: PlayNext called. CurrentIndex={currentIndex}, CinematicsCount={cinematics.Count}, activeInHierarchy={gameObject.activeInHierarchy}", this);
        }
        if (cinematics.Count == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(CinematicManager)}: PlayNext ignorado porque no hay cinemáticas.", this);
            }
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
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(CinematicManager)}: nextIndex fuera de rango ({nextIndex}).", this);
            }
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(CinematicManager)}: SetCurrentIndex from {currentIndex} to {nextIndex}. playNew={playNew}", this);
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
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(CinematicManager)}: CinematicEntry null en índice {currentIndex}.", this);
            }
            return;
        }

        if (current.director != null)
        {
            current.director.gameObject.SetActive(true);
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"{nameof(CinematicManager)}: Director null en índice {currentIndex}.", this);
        }

        ApplyCameraPriority(currentIndex);
        onCinematicChanged?.Invoke(currentIndex);

        if (playNew)
        {
            if (debugLogs)
            {
                Debug.Log($"{nameof(CinematicManager)}: Play director={(current.director == null ? "null" : current.director.name)} at index={currentIndex}", this);
            }
            current.director?.Play();

            backgroundMusicInteractionPaused = false;
        }

        UpdateBackgroundMusicState(forceApply: true);
    }

    public void PauseBackgroundMusic()
    {
        backgroundMusicManuallyPaused = true;
        UpdateBackgroundMusicState(forceApply: true);
    }

    public void ResumeBackgroundMusic()
    {
        backgroundMusicManuallyPaused = false;
        UpdateBackgroundMusicState(forceApply: true);
    }

    // Helpers para enganchar en UnityEvents cuando entras/sales de una pregunta/interaction.
    public void OnInteractionStarted()
    {
        backgroundMusicInteractionPaused = true;
        UpdateBackgroundMusicState(forceApply: true);
    }

    public void OnInteractionEnded()
    {
        backgroundMusicInteractionPaused = false;
        UpdateBackgroundMusicState(forceApply: true);
    }

    private void UpdateBackgroundMusicState(bool forceApply)
    {
        if (backgroundMusicSource == null)
        {
            return;
        }

        bool cinematicPlaying = IsCurrentCinematicPlaying;
        bool isExceptionIndex = IsCurrentIndexBackgroundMusicException();

        bool allowOutsideCinematic = !pauseBackgroundMusicWhenNoCinematicPlaying;

        bool shouldPlay = !backgroundMusicManuallyPaused
            && !backgroundMusicInteractionPaused
            && !isExceptionIndex
            && (cinematicPlaying || allowOutsideCinematic);

        if (!forceApply && shouldPlay == lastDesiredBackgroundMusicState)
        {
            return;
        }

        lastDesiredBackgroundMusicState = shouldPlay;

        if (shouldPlay)
        {
            // UnPause mantiene el tiempo si venía pausada.
            if (!backgroundMusicSource.isPlaying)
            {
                if (backgroundMusicSource.timeSamples > 0)
                {
                    backgroundMusicSource.UnPause();
                }
                else
                {
                    backgroundMusicSource.Play();
                }
            }

            if (debugLogs)
            {
                Debug.Log($"{nameof(CinematicManager)}: BackgroundMusic -> PLAY (index={currentIndex}, cinematicPlaying={cinematicPlaying}, exception={isExceptionIndex}, manualPause={backgroundMusicManuallyPaused}, interactionPause={backgroundMusicInteractionPaused})", this);
            }
        }
        else
        {
            if (backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Pause();
            }

            if (debugLogs)
            {
                Debug.Log($"{nameof(CinematicManager)}: BackgroundMusic -> PAUSE (index={currentIndex}, cinematicPlaying={cinematicPlaying}, exception={isExceptionIndex}, manualPause={backgroundMusicManuallyPaused}, interactionPause={backgroundMusicInteractionPaused})", this);
            }
        }
    }

    private void RebuildDirectorIndex()
    {
        directorToIndex.Clear();

        for (int i = 0; i < cinematics.Count; i++)
        {
            var entry = cinematics[i];
            if (entry == null || entry.director == null)
            {
                continue;
            }

            // Si se repite un director por error, nos quedamos con el primer índice.
            if (!directorToIndex.ContainsKey(entry.director))
            {
                directorToIndex.Add(entry.director, i);
            }
        }
    }

    private void HookDirectorEvents()
    {
        if (directorEventsHooked)
        {
            return;
        }

        foreach (var kvp in directorToIndex)
        {
            var director = kvp.Key;
            if (director == null)
            {
                continue;
            }

            director.played += OnDirectorPlayed;
            director.paused += OnDirectorPaused;
            director.stopped += OnDirectorStopped;
        }

        directorEventsHooked = true;
    }

    private void UnhookDirectorEvents()
    {
        if (!directorEventsHooked)
        {
            return;
        }

        foreach (var kvp in directorToIndex)
        {
            var director = kvp.Key;
            if (director == null)
            {
                continue;
            }

            director.played -= OnDirectorPlayed;
            director.paused -= OnDirectorPaused;
            director.stopped -= OnDirectorStopped;
        }

        directorEventsHooked = false;
    }

    private void OnDirectorPlayed(PlayableDirector director)
    {
        if (debugLogs)
        {
            int idx = director != null && directorToIndex.TryGetValue(director, out int found) ? found : -1;
            Debug.Log($"{nameof(CinematicManager)}: Director played. index={idx}, currentIndex={currentIndex}", this);
        }

        UpdateBackgroundMusicState(forceApply: true);
    }

    private void OnDirectorPaused(PlayableDirector director)
    {
        if (debugLogs)
        {
            int idx = director != null && directorToIndex.TryGetValue(director, out int found) ? found : -1;
            Debug.Log($"{nameof(CinematicManager)}: Director paused. index={idx}, currentIndex={currentIndex}", this);
        }

        UpdateBackgroundMusicState(forceApply: true);
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (debugLogs)
        {
            int idx = director != null && directorToIndex.TryGetValue(director, out int found) ? found : -1;
            Debug.Log($"{nameof(CinematicManager)}: Director stopped. index={idx}, currentIndex={currentIndex}", this);
        }

        UpdateBackgroundMusicState(forceApply: true);
    }

    private bool IsCurrentIndexBackgroundMusicException()
    {
        if (backgroundMusicPausedCinematicIndices == null || backgroundMusicPausedCinematicIndices.Length == 0)
        {
            return false;
        }

        int index = currentIndex;
        for (int i = 0; i < backgroundMusicPausedCinematicIndices.Length; i++)
        {
            if (backgroundMusicPausedCinematicIndices[i] == index)
            {
                return true;
            }
        }

        return false;
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

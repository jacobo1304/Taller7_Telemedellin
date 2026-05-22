using UnityEngine;

public class AudioLibrary : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Rating State Clips")]
    [SerializeField] private AudioClip[] muyMalClips = new AudioClip[0];
    [SerializeField] private AudioClip[] malClips = new AudioClip[0];
    [SerializeField] private AudioClip[] promedioClips = new AudioClip[0];
    [SerializeField] private AudioClip[] bienClips = new AudioClip[0];
    [SerializeField] private AudioClip[] muyBienClips = new AudioClip[0];

    [Header("Special Clips")]
    [SerializeField] private AudioClip tickClip;
    [SerializeField] private AudioClip stayAtTopClip;
    [SerializeField] private AudioClip stayAtBottomClip;

    [Header("Volume")]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayTick()
    {
        PlayClip(tickClip, "Tick");
    }

    public void PlayStayAtTop()
    {
        PlayClip(stayAtTopClip, "StayAtTop");
    }

    public void PlayStayAtBottom()
    {
        PlayClip(stayAtBottomClip, "StayAtBottom");
    }

    public void PlayState(RatingState state)
    {
        AudioClip[] clips = GetStateClips(state);
        PlayRandomClip(clips, state.ToString());
    }

    private AudioClip[] GetStateClips(RatingState state)
    {
        switch (state)
        {
            case RatingState.MuyMal:
                return muyMalClips;
            case RatingState.Mal:
                return malClips;
            case RatingState.Promedio:
                return promedioClips;
            case RatingState.Bien:
                return bienClips;
            case RatingState.MuyBien:
                return muyBienClips;
            default:
                return null;
        }
    }

    private void PlayRandomClip(AudioClip[] clips, string label)
    {
        if (clips == null || clips.Length == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AudioLibrary)}: No clips for {label}.");
            }
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlayClip(clip, label);
    }

    private void PlayClip(AudioClip clip, string label)
    {
        if (audioSource == null || clip == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AudioLibrary)}: Missing AudioSource or clip for {label}.");
            }
            return;
        }

        audioSource.PlayOneShot(clip, sfxVolume);

        if (debugLogs)
        {
            Debug.Log($"{nameof(AudioLibrary)}: Playing {label}.");
        }
    }
}

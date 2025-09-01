using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip normalWorldClip;
    [SerializeField] private AudioClip otherWorldClip;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Suscribite al evento de cambio de mundo
        WorldManager.OnWorldChanged += HandleWorldChanged;
        PlayMusic(WorldManager.Instance.CurrentWorld);
    }

    private void OnDestroy()
    {
        WorldManager.OnWorldChanged -= HandleWorldChanged;
    }

    private void HandleWorldChanged(WorldState newWorld)
    {
        PlayMusic(newWorld);
    }

    public void PlayMusic(WorldState world)
    {
        if (audioSource == null) return;

        AudioClip targetClip = (world == WorldState.Normal) ? normalWorldClip : otherWorldClip;
        if (audioSource.clip == targetClip && audioSource.isPlaying) return; // Ya está sonando

        audioSource.clip = targetClip;
        audioSource.loop = true;
        audioSource.Play();
    }
}

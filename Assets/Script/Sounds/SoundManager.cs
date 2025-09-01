using Assets.Script.Sounds;
using System;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundsSO SO;
    public static SoundManager Instance { get; private set; }

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject); // Opcional si lo querés persistente
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1f)
    {
        if (Instance == null)
        {
            Debug.LogError("[SoundManager] No existe una instancia en escena.");
            return;
        }

        if (Instance.SO == null)
        {
            Debug.LogError("[SoundManager] SoundsSO no asignado.");
            return;
        }

        if ((int)sound < 0 || (int)sound >= Instance.SO.sounds.Length)
        {
            Debug.LogError($"[SoundManager] SoundType {sound} fuera de rango.");
            return;
        }

        SoundList soundList = Instance.SO.sounds[(int)sound];

        if (soundList.sounds == null || soundList.sounds.Length == 0)
        {
            Debug.LogWarning($"[SoundManager] No hay AudioClips asignados para el sonido {sound}.");
            return;
        }

        AudioClip randomClip = soundList.sounds[UnityEngine.Random.Range(0, soundList.sounds.Length)];

        if (source != null)
        {
            source.outputAudioMixerGroup = soundList.mixer;
            source.clip = randomClip;
            source.volume = volume * soundList.volume;
            source.Play();
        }
        else
        {
            Instance.audioSource.outputAudioMixerGroup = soundList.mixer;
            Instance.audioSource.PlayOneShot(randomClip, volume * soundList.volume);
        }
    }
}

[Serializable]
public struct SoundList
{
    [HideInInspector] public string name;
    [Range(0, 1)] public float volume;
    public AudioMixerGroup mixer;
    public AudioClip[] sounds;
}

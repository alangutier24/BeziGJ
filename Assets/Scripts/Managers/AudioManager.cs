using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// The single entry point for all game audio.
// Put this on one GameObject in your scene, assign the mixer + groups, done.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer (expose params named MasterVol, MusicVol, SFXVol)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("SFX pool")]
    [SerializeField] private int poolSize = 12;

    [Header("Music")]
    [SerializeField] private float defaultFade = 1f;

    private AudioSourcePool sfxPool;
    private AudioSource musicA, musicB;   // crossfade between these two
    private AudioSource activeMusic;
    private Coroutine musicFade;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);    // safe even in a single-scene game

        sfxPool = new AudioSourcePool(transform, poolSize);
        musicA = CreateMusicSource("MusicA");
        musicB = CreateMusicSource("MusicB");
        activeMusic = musicA;

        LoadVolumes();
    }

    private AudioSource CreateMusicSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.outputAudioMixerGroup = musicGroup;
        return src;
    }

    // ---------- SFX ----------
    // Returns the source so you can stop looping sounds yourself.

    public AudioSource PlaySFX(SoundDefinition sound)
    {
        var src = Prepare(sound);
        if (src == null) return null;
        src.spatialBlend = 0f;            // 2D
        src.Play();
        return src;
    }

    public AudioSource PlaySFX3D(SoundDefinition sound, Vector3 position)
    {
        var src = Prepare(sound);
        if (src == null) return null;
        src.transform.position = position;
        src.spatialBlend = 1f;            // 3D
        src.Play();
        return src;
    }

    private AudioSource Prepare(SoundDefinition sound)
    {
        var clip = sound?.PickClip();
        if (clip == null) return null;
        var src = sfxPool.Get();
        src.clip = clip;
        src.volume = sound.volume;
        src.pitch = sound.PickPitch();
        src.loop = sound.loop;            // for looping ambience keep the returned ref to Stop() it
        src.outputAudioMixerGroup = sound.mixerGroup;
        return src;
    }

    // ---------- Music ----------

    public void PlayMusic(SoundDefinition track, float? fade = null)
    {
        var clip = track?.PickClip();
        if (clip == null) 
        {
            Debug.Log("Clip is null");
            return;
        }

        if (activeMusic.clip == clip && activeMusic.isPlaying) return; // already on it

        float f = fade ?? defaultFade;
        var next = activeMusic == musicA ? musicB : musicA;

        next.clip = clip;
        next.volume = 0f;
        next.pitch = 1f;
        next.Play();

        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(Crossfade(activeMusic, next, track.volume, f));
        activeMusic = next;
    }

    public void StopMusic(float fade = 1f)
    {
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(FadeOut(activeMusic, fade));
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float targetVol, float dur)
    {
        float t = 0f, startFrom = from.volume;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;  // keeps working while paused
            float k = dur <= 0f ? 1f : t / dur;
            from.volume = Mathf.Lerp(startFrom, 0f, k);
            to.volume   = Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }
        from.Stop();
        from.volume = 0f;
        to.volume = targetVol;
        musicFade = null;
    }

    private IEnumerator FadeOut(AudioSource src, float dur)
    {
        float t = 0f, start = src.volume;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, dur <= 0f ? 1f : t / dur);
            yield return null;
        }
        src.Stop();
        musicFade = null;
    }

    // ---------- Volume (0..1 slider -> dB) ----------

    public void SetMasterVolume(float v) => SetVolume("MasterVol", v);
    public void SetMusicVolume(float v)  => SetVolume("MusicVol", v);
    public void SetSFXVolume(float v)     => SetVolume("SFXVol", v);

    private void SetVolume(string param, float linear)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(param, dB);
        PlayerPrefs.SetFloat(param, linear);
    }

    private void LoadVolumes()
    {
        SetVolume("MasterVol", PlayerPrefs.GetFloat("MasterVol", 1f));
        SetVolume("MusicVol",  PlayerPrefs.GetFloat("MusicVol", 1f));
        SetVolume("SFXVol",    PlayerPrefs.GetFloat("SFXVol", 1f));
    }
}

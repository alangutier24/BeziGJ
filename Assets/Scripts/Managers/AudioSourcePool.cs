using System.Collections.Generic;
using UnityEngine;

// Plain C# helper (not a MonoBehaviour). Owned by AudioManager.
// Hands out AudioSources that aren't currently playing; a source frees
// itself automatically once its clip finishes (isPlaying goes false).
public class AudioSourcePool
{
    private readonly List<AudioSource> sources = new();
    private readonly Transform parent;

    public AudioSourcePool(Transform parent, int initialSize)
    {
        this.parent = parent;
        for (int i = 0; i < initialSize; i++) sources.Add(Create());
    }

    private AudioSource Create()
    {
        var go = new GameObject("PooledAudioSource");
        go.transform.SetParent(parent);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        return src;
    }

    // Returns a free source; grows the pool rather than cutting a live sound.
    public AudioSource Get()
    {
        foreach (var s in sources)
            if (!s.isPlaying) return s;

        var extra = Create();
        sources.Add(extra);
        return extra;
    }
}

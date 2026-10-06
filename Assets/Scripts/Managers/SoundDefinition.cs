using UnityEngine;
using UnityEngine.Audio;

// Data-only asset. Create via: Assets > Create > Audio > Sound Definition.
// One asset per sound (or per group of interchangeable clips).
[CreateAssetMenu(fileName = "Sound", menuName = "Audio/Sound Definition")]
public class SoundDefinition : ScriptableObject
{
    [Tooltip("One clip, or several — a random one is picked each play.")]
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Random pitch between x and y. Set both to 1 for no variation.")]
    public Vector2 pitchRange = new Vector2(1f, 1f);

    public bool loop = false;

    [Tooltip("Assign your SFX or Music group from the AudioMixer.")]
    public AudioMixerGroup mixerGroup;

    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public float PickPitch() => Random.Range(pitchRange.x, pitchRange.y);
}

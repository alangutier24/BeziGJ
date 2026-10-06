using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;            
using UnityEngine.Rendering.Universal; 
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class ShaderManager : MonoBehaviour
{
    public static ShaderManager Instance { get; private set; }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    readonly List<IShaderSource> sources = new();

    [SerializeField] Volume volume;
    Vignette vignette; ChromaticAberration chroma; ColorAdjustments color;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        volume = null; vignette = null; chroma = null; color = null;

        var volGo = GameObject.Find("Volume");
        if (!volGo) return;

        volume = volGo.GetComponent<Volume>();
        if (!volume) return;

        if (!volume.profile.TryGet(out vignette) ||
            !volume.profile.TryGet(out chroma)   ||
            !volume.profile.TryGet(out color)) return;

        Debug.Log($"[SM] scene {scene.name} | volume {(volume ? volume.name : "NULL")} | " +
            $"vignette {vignette != null} chroma {chroma != null} color {color != null}");


        vignette.color.value = Color.black;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {

    }

    public void Register(IShaderSource source)
    {
        sources.Add(source);
    }

    public void Unregister(IShaderSource source)
    {
        sources.Remove(source);
    }

    void LateUpdate()
    {
        if(!volume) return;
        float vignettemax = 0f, vignettesmootmax = 0f, chromamax = 0f, saturation = 0f;

        foreach (var s in sources)
        {
            vignettemax      = Mathf.Max(vignettemax, s.VignetteTotal);
            vignettesmootmax = Mathf.Max(vignettesmootmax, s.VignetteSmooth);
            chromamax        = Mathf.Max(chromamax, s.Chroma);
            if (Mathf.Abs(s.ColorSaturation) > Mathf.Abs(saturation))
                saturation = s.ColorSaturation;
        }

        vignette.intensity.Override(vignettemax);
        vignette.smoothness.Override(vignettesmootmax);
        chroma.intensity.Override(chromamax);
        color.saturation.Override(saturation);
    }


}

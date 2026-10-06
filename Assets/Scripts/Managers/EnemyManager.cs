using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;            
using UnityEngine.Rendering.Universal; 
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EnemyManager : MonoBehaviour, IShaderSource
{

    public static EnemyManager Instance {get; private set;}

    readonly List<IAlertSource> sources = new();
    
    public float VignetteTotal {get; private set;}
    public float Chroma {get; private set;}
    public float ColorSaturation {get; private set;}
    public float VignetteSmooth {get; private set;}

    [SerializeField] public Image bar;

    [SerializeField] Cop[] cops;

    Dictionary<Cop, Transform> copsTransforms = new Dictionary<Cop, Transform>();

    [SerializeField] Player player;

    [SerializeField]
    public GameObject caughtPanel;

    public bool dead = false;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ShaderManager.Instance.Register(this);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ShaderManager.Instance.Unregister(this);
    }
    void Awake()
    {
        if(Instance != null && Instance != this) 
        { Destroy(gameObject); return;}
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bar = null;
        var barGo = GameObject.Find("SightBar");

        if (!barGo) return;

        bar    = barGo.GetComponent<Image>();

        if (!bar) return;

        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = (int)Image.OriginHorizontal.Left;

        cops = FindObjectsByType<Cop>(FindObjectsSortMode.None);

        foreach(Cop cp in cops)
        {
            cp.enabled = false;
            copsTransforms[cp] = cp.transform;
        }

        player = GameObject.Find("PF Pixel Character").GetComponent<Player>();

        caughtPanel = GameObject.Find("CaughtCanvas");
        caughtPanel.SetActive(false);

    }

    public void Register(IAlertSource s)
    {
        sources.Add(s);
        Debug.Log($"Registered {((MonoBehaviour)s).name}. Total: {sources.Count}");
    }
    
    public void Unregister(IAlertSource s) => sources.Remove(s);

    public void EnableCops()
    {
        foreach(Cop cp in cops)
        {{
            cp.enabled = true;
        }}
    }

    void LateUpdate()
    {
        float max = 0f;
        foreach (var s in sources)
            max = Mathf.Max(max, s.AlertLevel);

        float t = Mathf.Clamp01(max / 175f);
        

        if (bar)
        {
            bar.enabled = t > 0f;
            bar.fillAmount = t;
        }

        float pulse = Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f;
        float peak  = Mathf.Lerp(0.35f, 0.6f, pulse);
        VignetteTotal   = t * peak;
        VignetteSmooth  = t * peak;
        Chroma          = t;
        ColorSaturation = -70f * t;
    }

    public void PlayerDies()
    {
        if(!dead)
        {
            player.Die();
            dead = true;

            foreach(Cop cp in cops)
            {
                cp.transform.position = copsTransforms[cp].position;
            }

            StartCoroutine(ResetPlayerDeath(2.5f));
        }
    }

    IEnumerator ResetPlayerDeath(float duration)
    {
        float elapsed = 0f;
        while(elapsed < duration){
            elapsed += Time.deltaTime;
            yield return null;
        }
        dead = false;
    }

    public void SetCaughtPanel(bool active)
    {
        if(active)
        {
            Time.timeScale = 0.15f;
            caughtPanel.GetComponentInChildren<Canvas>().sortingOrder = 10;
        }
        else
        {
            caughtPanel.GetComponentInChildren<Canvas>().sortingOrder = 0;
        }
        caughtPanel.SetActive(active);
    }
    
}

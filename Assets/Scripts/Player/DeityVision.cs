using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; 
using UnityEngine.UI;

public class DeityVision : MonoBehaviour, IShaderSource
{
    public static DeityVision Instance { get; private set; }

    public float VignetteTotal {get; private set;}
    public float Chroma {get; private set;}
    public float ColorSaturation {get; private set;}
    public float VignetteSmooth {get; private set;}

    void OnEnable() => ShaderManager.Instance.Register(this);
    void OnDisable() => ShaderManager.Instance.Unregister(this);

    [SerializeField] Transform player;
    [SerializeField] UnityEngine.InputSystem.Key toggleKey = UnityEngine.InputSystem.Key.Q;
    [SerializeField] bool unlockedAtStart = false; // for testing
    [SerializeField] Volume visionVolume;   // Global Volume, weight driven by radius
    [SerializeField] Image energyFill;      // optional, Image type = Filled

    [Header("Radius")]
    [SerializeField] float maxRadius = 4f;
    [SerializeField] float expandSpeed = 8f;

    [Header("Energy")]
    [SerializeField] float maxEnergy = 5f;                // seconds of continuous use
    [SerializeField] float regenRate = 0.5f;              // energy per second while off
    [SerializeField, Range(0f, 1f)] float reactivateThreshold = 0.25f; // after running dry

    [SerializeField] public Image manaBar;

    public bool Unlocked { get; set; }      // set true after meeting the first deity
    public bool Active { get; private set; }
    public float Radius => radius;
    public float Energy01 => energy / maxEnergy;
    public event Action<bool> OnVisionChanged;

    float energy;
    float radius;
    bool exhausted;

    static readonly int CenterID = Shader.PropertyToID("_VisionCenter");
    static readonly int RadiusID = Shader.PropertyToID("_VisionRadius");

    void Awake()
    {
        Instance = this;
        energy = maxEnergy;
        Unlocked = unlockedAtStart;
        Shader.SetGlobalFloat(RadiusID, 0f);

        manaBar.type = Image.Type.Filled;
        manaBar.fillMethod = Image.FillMethod.Horizontal;
        manaBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        manaBar.fillAmount = 0;

    }

    void HandleInput()
    {
        if (!Unlocked || Keyboard.current == null) return;
        
        if (!Keyboard.current[toggleKey].wasPressedThisFrame) return;

        if (Active) SetActive(false);
        else if (!exhausted) SetActive(true);
    }

    void SetActive(bool value)
    {
        if (Active == value) return;
        Active = value;
        OnVisionChanged?.Invoke(value);
    }

    void Update()
    {
        HandleInput();

        manaBar.enabled = energy > 0f;
        if (Active)
        {
            energy -= Time.deltaTime;
            manaBar.fillAmount = energy;
            if (energy <= 0f)
            {
                energy = 0f;
                exhausted = true;
                SetActive(false);
            }

            ColorSaturation = Mathf.Lerp(0f, -180f, radius / maxRadius);

            VignetteSmooth = Mathf.Lerp(0f, 1f, radius / maxRadius);

            VignetteTotal = Mathf.Lerp(0f, 0.3f, radius / maxRadius);
        }
        else
        {
            energy = Mathf.Min(maxEnergy, energy + regenRate * Time.deltaTime);
            if (exhausted && Energy01 >= reactivateThreshold) exhausted = false;
            ColorSaturation = 0;
            VignetteTotal = 0;
        }

        radius = Mathf.MoveTowards(radius, Active ? maxRadius : 0f, expandSpeed * Time.deltaTime);

        Shader.SetGlobalVector(CenterID, player.position);
        Shader.SetGlobalFloat(RadiusID, radius);

        //if (visionVolume) visionVolume.weight = radius / maxRadius;
        if (energyFill) energyFill.fillAmount = Energy01;
    }

    // Use this to gate interactions (key, weapon, deity) so they only work while revealed.
    public bool IsRevealed(Vector3 worldPos)
    {
        Vector3 d = worldPos - player.position;
        d.y = 0f;
        return d.sqrMagnitude <= radius * radius;
    }

    public void UnlockSkill()
    {
        Unlocked = true;
    }
}
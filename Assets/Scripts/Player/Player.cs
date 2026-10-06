using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class Player : MonoBehaviour, IDamagable, IShaderSource
{
    public float MaxHealth {get; set;} = 100.0f;
    public float CurrentHealth {get; set;} = 100.0f;

    public float VignetteTotal {get; private set;}
    public float Chroma {get; private set;}
    public float ColorSaturation {get; private set;}
    public float VignetteSmooth {get; private set;}

    [SerializeField]
    private float _castDistance = 5f; 
    [SerializeField]
    private Vector3 _rayCastOffset = new Vector3(0, 0f, 0);

    public Camera cam;

    public Transform targetPos;

    public Transform fpsPos;
    
    [SerializeField] Vector3 targetEuler = Vector3.zero;

    [SerializeField]
    public Animator animator;

    public CharacterController cc;
    public float moveSpeed = 5f;

    public int IsWalkingHash = Animator.StringToHash("isWalking");

    public float sharpness = 8f;

    private float dealDamage = 10f;

    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public IndoorCamera indoorCamera;

    public bool dialogueBlock = false;

    public Transform restartPoint;

    public Animator restartAnim;

    private bool isHiden = true;

    [SerializeField] public Image bar;

    public PlayerStateMachine stateMachine {get; set;}

    public PlayerIdleState idleState{ get; set;}

    public PlayerInvestigateState investigateState{ get; set;}

    public PlayerStealthState stealthState{ get; set;}

    public PlayerAttackState attackState { get; set;}

    private void Awake()
    {
        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine);
        investigateState = new PlayerInvestigateState(this, stateMachine);
        stealthState = new PlayerStealthState(this, stateMachine);
        attackState = new PlayerAttackState(this, stateMachine);
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;

        stateMachine.Initialize(investigateState);
    }

    private void Update()
    {
        stateMachine.CurrentPlayerState.FrameUpdate();
        stateMachine.CurrentPlayerState.PlayerInput();
    }

    private void PhysicsUpdate()
    {
        Debug.Log("Physics Update");
        stateMachine.CurrentPlayerState.PhysicsUpdate();
    }

    void OnEnable()
    {
        ShaderManager.Instance.Register(this);
    }

    void OnDisable()
    {
        ShaderManager.Instance.Unregister(this);
    }

    public void Die() {

        Debug.Log("Player: Die");

        if(!isHiden)
            restartAnim.SetBool("ScrollOut", false);

        if(isHiden)
        {
            restartAnim.SetBool("IsHiden", false);
            isHiden = false;
        }
        StartCoroutine(ScrollIn(2.5f));
    }

    public void Damage(float damageAmount)
    {
        if(CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Shoot()
    {       
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log("Raycast");
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            Debug.Log(hit.collider.name);
            if(enemy != null)
            {
                Debug.Log("Enemy Shot!");
                enemy.Damage(dealDamage);
            }
        }
    }

    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        stateMachine.CurrentPlayerState.AnimationTriggerEvent(triggerType);
    }

    public enum AnimationTriggerType
    {
        TakeDamage, 
        PlayerFootstepSound
    }

    public void IndoorCameraUpdate()
    {
        indoorCamera.CameraUpdate();
    }

    public void IndoorCameraStart()
    {
        indoorCamera.Start();
    }

    public void SetCameraFPS()
    {
        StartCoroutine(MoveCamera(1.5f));
    }

    IEnumerator ScrollIn(float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            if(elapsed > duration / 2f)
            {
                cc.enabled = false;
                transform.position = restartPoint.position;
                cc.enabled = true;
            }
            yield return null;
        }
        EnemyManager.Instance.SetCaughtPanel(false);
        restartAnim.SetBool("ScrollOut", true);
        Time.timeScale = 1f;
    }
       IEnumerator MoveCamera(float duration) {
        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;
        Quaternion toRot = Quaternion.Euler(targetEuler);
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = ease.Evaluate(elapsed / duration);
            cam.transform.position = Vector3.Lerp(fromPos, fpsPos.position, t);
            cam.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }
        cam.transform.position = fpsPos.transform.position;
        cam.transform.rotation = toRot;
    }
}

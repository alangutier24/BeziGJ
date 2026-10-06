using UnityEngine;
using System.Collections;

public class Enemy :  EnemyBase, IDamagable, IShaderSource
{

    public float VignetteTotal {get; private set;}
    public float Chroma {get; private set;}
    public float ColorSaturation {get; private set;}
    public float VignetteSmooth {get; private set;}

    void OnEnable() => ShaderManager.Instance.Register(this);
    void OnDisable() => ShaderManager.Instance.Unregister(this);

    public float MaxHealth {get; set;} = 100.0f;
    public float CurrentHealth {get; set;} = 100.0f;
    private float contactDistance;

    private float shootingRange = 5f;

    Player player;

    Enemy[] allEnemies;

    public float turnSpeed = 7f;
 
    [SerializeField]
    private float sightTimer = 0f;

    [SerializeField]
    private float maxSightTimer = 10f;

    [SerializeField]
    private float wanderRadius = 7f;

    [SerializeField]
    Animator anim;

    [SerializeField]
    private float alertRadius = 10f;

    private float shootelapsed = 0;

    public float shootWait = 1.5f;
    
    [SerializeField]
    private float catchGap = 0.15f; 

    [SerializeField] float replanInterval = 0.15f;

    private bool reachedLastKnown = false;

    float replanTimer;

    private bool dead = false;

    public void Die()
    {
        anim.SetBool("Died", true);
        agent.isStopped = true;          // stops path following
        agent.ResetPath();               // clears the current destination
        agent.velocity = Vector3.zero;   // kills leftover momentum so it doesn't slide
        dead = true;
        StartCoroutine(DestroyMesh(20f));

    }

    IEnumerator DestroyMesh(float delay)
    {
        float elapsed = 0;
        while(elapsed < delay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        GameObject owner = rb.gameObject;
        Destroy(owner);
    }

    public void Damage(float damageAmount)
    {
        CurrentHealth -= damageAmount;
        anim.SetBool("GotShot", true);
        agent.isStopped = true;          // stops path following
        agent.ResetPath();               // clears the current destination
        agent.velocity = Vector3.zero;
        if(CanSeePlayer())
        {
            state = EnemyState.Shoot;
        }
        else
        {
            state = EnemyState.Search;
        }
        if(CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected override void Start()
    {
        base.Start();
        player = FindFirstObjectByType<Player>();  
        contactDistance = HorizontalRadius(FindCollider(transform)) + HorizontalRadius(FindCollider(playerTransform));
        agent.stoppingDistance = contactDistance;
        //exclamationMark.enabled = false;
        lastKnownPosition = playerTransform.position;
        agent.updateRotation  = true;
        base.Shooter = true;
        base.viewRange = 15f;
    }

    protected override EnemyState DecideState()
    {
        EnemyState next;

        if (CanSeeTarget)
        {
            next = EnemyState.Shoot;
        }
        else
        {
            next = EnemyState.Patrol;
        }

        return next;
    }

    protected override void TickState()
    {
        if(dead)return;
        switch(state)
        {
            case EnemyState.Patrol:
                Patrol();
                if(waitTimer > 0)
                {
                    anim.SetBool("IsIdle", true);
                    anim.SetBool("IsWalking", false);
                    anim.SetBool("GotShot", false);
                }
                else
                 {
                    anim.SetBool("IsIdle", false);
                    anim.SetBool("IsWalking", true);
                     anim.SetBool("GotShot", false);
                 }
            break;
            case EnemyState.Pursue:
                Pursue();
            break;
            case EnemyState.Search:
                SearchArea();
            break;
            case EnemyState.Shoot:
                ShootPlayer();
            break;
        }
    }

    protected override void Move(Vector3 destination)
    {
        base.Move(currentTarget.position);
    }

    protected override void OnStateChanged(EnemyState oldState, EnemyState newState)
    {
        base.OnStateChanged(oldState, newState);
        //exclamationMark.enabled = false;
        switch (newState)
        {
            case EnemyState.Pursue:
                if (oldState != EnemyState.Catch)
                {
                    AlertNearbyCops();
                }
            break;
            case EnemyState.Search:
                StartSearch();
            break;
            case EnemyState.Catch:
                agent.ResetPath();
            break;
        }
    }

    private void ShootPlayer()
    {


        SearchArea();
                
        float side = Vector3.Dot(transform.right, lookDir);
        // side > 0 → right
        // side < 0 → left
        // side ≈ 0 → straight ahead or behind
        if(side < 0)
            anim.SetBool("RunLeft", true);
        else if(side > 0)
        {
            anim.SetBool("RunRight", true);
        }
        else
        {
            anim.SetBool("IsWalking", true);
        }

        shootelapsed += Time.deltaTime;
        if(shootelapsed < shootWait) return;

        agent.isStopped = true;          // stops path following
        agent.ResetPath();               // clears the current destination
        agent.velocity = Vector3.zero;
        lookDir = player.transform.position - transform.position;

        Debug.Log("Player must have taken damage");

        anim.SetBool("IsShooting", true);

        FaceTarget(playerTransform.position, turnSpeed);
        player.attackState.Damage(10);
        shootelapsed = 0;
    }

        // Horizontal (XZ) radius of an upright collider, including scale
    static float HorizontalRadius(Collider c)
    {
        if (c == null) return 0f;

        Vector3 s = c.transform.lossyScale;
        float xz = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));

        switch (c)
        {
            case CharacterController cc: return (cc.radius + cc.skinWidth) * xz;
            case CapsuleCollider cap:    return cap.radius * xz;
            case SphereCollider sph:     return sph.radius * Mathf.Max(xz, Mathf.Abs(s.y));
            default:
                Vector3 e = c.bounds.extents;
                return Mathf.Min(e.x, e.z);
        }
    }

    static Collider FindCollider(Transform t)
    {
        Collider c = t.GetComponent<Collider>();
        return c != null ? c : t.GetComponentInChildren<Collider>();
    }

    bool inRange()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        float reach = contactDistance + catchGap;
        return toPlayer.sqrMagnitude <= reach * reach;
    }

    void Pursue()
    {
        anim.SetBool("IsWalking", true);
        anim.SetBool("IsShooting", false);
        agent.SetDestination(playerTransform.position);
        lastKnownPosition = playerTransform.position;
        replanTimer = replanInterval;
        
    }


    void SearchArea()
    {
        if (!reachedLastKnown)
        {
            reachedLastKnown = HasArrived();
            return;
        }

        if (!HasArrived())
        {
            return;
        }
        anim.SetBool("IsShooting", false);
        anim.SetBool("IsWalking", true);
        Vector3 randomPoint = lastKnownPosition + Random.insideUnitSphere * wanderRadius;
        if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out UnityEngine.AI.NavMeshHit hit, wanderRadius, UnityEngine.AI.NavMesh.AllAreas) &&
            agent.CalculatePath(hit.position, navMesh) &&
            navMesh.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
        {
            Move(hit.position);
            //exclamationMark.transform.parent.position = hit.position;
        }
    }

    public void Alert(Vector3 position)
    {
        if (CanSeeTarget) return;

        lastKnownPosition = position;

        if (state == EnemyState.Search)
        {
            StartSearch(); 
        }
    }


    void AlertNearbyCops()
    {
        foreach (Enemy enmy in allEnemies)
        {
            if (enmy == this) continue;

            if ((enmy.transform.position - transform.position).sqrMagnitude <= alertRadius * alertRadius)
            {
                enmy.Alert(lastKnownPosition);
            }
        }
    }


    void StartSearch()
    {
        reachedLastKnown = false;
        Move(lastKnownPosition);
        //exclamationMark.enabled = true;
        //.transform.parent.position = lastKnownPositionition;
    }



}

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class Cop : EnemyBase, IAlertSource
{

    public float AlertLevel {get; private set;}

    [Header("Screen FX at full sight")]
    [SerializeField] float maxVignette = 1f;
    [SerializeField] float maxVignetteSmooth = 0.5f;
    [SerializeField] float maxChroma = 1f;
    [SerializeField] float maxSaturation = 180f;

    [SerializeField]
    private float catchGap = 0.15f; // max space between the two colliders to catch

    private float contactDistance; // center-to-center distance when the colliders touch
    
    [SerializeField]
    private float wanderRadius = 7f;

    //[SerializeField]
    //private SpriteRenderer exclamationMark;

    [SerializeField]
    private float sightTimer = 0f;

    [SerializeField]
    private float maxSightTimer = 10f; // full meter = catch; also the longest search (drains 1/s)

    [SerializeField]
    private float fillTime = 5f; // seconds of continuous sight to fill the meter

    [SerializeField, Range(0f, 1f)]
    private float alertFill = 0.5f; // meter fraction given to alerted cops

    [SerializeField]
    private float alertRadius = 10f;

    public bool Sus = false;

    private bool reachedLastKnown = false;

    private static readonly List<Cop> allCops = new List<Cop>();

    [SerializeField] float replanInterval = 0.15f;

    public Vector3 lastPlannedPos;

    float replanTimer;

    void OnEnable()
    {
        EnemyManager.Instance.Register(this);
    }

    void OnDisable()
    {
        EnemyManager.Instance.Unregister(this);
    }


    protected override void Start()
    {
        base.Start();
        contactDistance = HorizontalRadius(FindCollider(transform)) + HorizontalRadius(FindCollider(playerTransform));
        agent.stoppingDistance = contactDistance;
        //exclamationMark.enabled = false;
        lastPlannedPos = playerTransform.position;
        lookDir = transform.right;
        agent.updateUpAxis  = false;
        agent.updateRotation = false;
    }

    [SerializeField] private float alertGainRate = 60f;

    protected override EnemyState DecideState()
    {
        EnemyState next;

        if (CanSeeTarget)
        {
            sightTimer = Mathf.Min(sightTimer + Time.deltaTime * maxSightTimer / fillTime, maxSightTimer);
            next = inRange() || sightTimer >= maxSightTimer ? EnemyState.Catch : EnemyState.Pursue;
            AlertLevel += sightTimer * alertGainRate * Time.deltaTime;
        }
        else
        {
            // Lost sight (obstacle or out of range): drain from the first frame
            sightTimer = Mathf.Max(0f, sightTimer - Time.deltaTime);
            AlertLevel = sightTimer;
            next = sightTimer > 0f ? EnemyState.Search : EnemyState.Patrol;
        }

        Sus = !CanSeeTarget && sightTimer > 0f;
        return next;
    }

    protected override void TickState()
    {
        switch(state)
        {
            case EnemyState.Patrol:
                Patrol();
            break;
            case EnemyState.Pursue:
                Pursue();
            break;
            case EnemyState.Search:
                SearchArea();

            break;
            case EnemyState.Catch:
                CatchPlayer();
            break;
        }
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

    void StartSearch()
    {
        reachedLastKnown = false;
        Move(lastKnownPosition);
        //exclamationMark.enabled = true;
        //.transform.parent.position = lastKnownPosition;
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

        Vector3 randomPoint = lastKnownPosition + Random.insideUnitSphere * wanderRadius;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas) &&
            agent.CalculatePath(hit.position, navMesh) &&
            navMesh.status == NavMeshPathStatus.PathComplete)
        {
            Move(hit.position);
            //exclamationMark.transform.parent.position = hit.position;
        }
    }

    void CatchPlayer()
    {
        EnemyManager.Instance.SetCaughtPanel(true);
        StartCoroutine(CallEMToKillPlayer(2.5f));
    }

    void Pursue()
    {
        agent.SetDestination(playerTransform.position);
        lastPlannedPos = playerTransform.position;
        replanTimer = replanInterval;
        
    }

    bool inRange()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        float reach = contactDistance + catchGap;
        return toPlayer.sqrMagnitude <= reach * reach;
    }

    static Collider FindCollider(Transform t)
    {
        Collider c = t.GetComponent<Collider>();
        return c != null ? c : t.GetComponentInChildren<Collider>();
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

    public void Alert(Vector3 position)
    {
        if (CanSeeTarget) return;

        lastKnownPosition = position;
        sightTimer = Mathf.Max(sightTimer, maxSightTimer * alertFill); // never lowers its own meter

        if (state == EnemyState.Search)
        {
            StartSearch(); 
        }
    }

    void AlertNearbyCops()
    {
        foreach (Cop cop in allCops)
        {
            if (cop == this) continue;

            if ((cop.transform.position - transform.position).sqrMagnitude <= alertRadius * alertRadius)
            {
                cop.Alert(lastKnownPosition);
            }
        }
    }

    IEnumerator CallEMToKillPlayer(float duration)
    {
        float elapsed = 0;
        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        EnemyManager.Instance.PlayerDies();
    }

#if UNITY_EDITOR
    protected override Color GizmoColor()
    {
        return Sus ? Color.orange : base.GizmoColor();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (Sus)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.3f);
        }
    }
#endif

}